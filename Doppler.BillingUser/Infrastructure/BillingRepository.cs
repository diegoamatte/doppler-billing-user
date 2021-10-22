using Dapper;
using Doppler.AccountPlans.Utils;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class BillingRepository : IBillingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IEncryptionService _encryptionService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ISapService _sapService;
        private readonly IUserRepository _userRepository;

        private const int UserAccountType = 1;
        private const string ApprovedAccountingEntryStatus = "Approved";
        private const string InvoiceAccountingEntryTypeDescription = "Invoice";
        private const string CCPaymentAccountingEntryTypeDescription = "CC Payment";
        private const string InvoiceAccountEntryType = "I";
        private const string PaymentAccountEntryType = "P";
        private const string PaymentPaymentEntryType = "P";

        public BillingRepository(IDatabaseConnectionFactory connectionFactory,
            IEncryptionService encryptionService,
            IPaymentGateway paymentGateway,
            ISapService sapService,
            IUserRepository userRepository)
        {
            _connectionFactory = connectionFactory;
            _encryptionService = encryptionService;
            _paymentGateway = paymentGateway;
            _sapService = sapService;
            _userRepository = userRepository;
        }

        public async Task<BillingInformation> GetBillingInformation(string email)
        {
            using (IDbConnection connection = await _connectionFactory.GetConnection())
            {
                var results = await connection.QueryAsync<BillingInformation>(@"
SELECT
    U.BillingFirstName AS Firstname,
    U.BillingLastName AS Lastname,
    U.BillingAddress AS Address,
    U.BillingCity AS City,
    isnull(S.StateCode, '') AS Province,
    isnull(CO.Code, '') AS Country,
    U.BillingZip AS ZipCode,
    U.BillingPhone AS Phone
FROM
    [User] U
    LEFT JOIN [State] S ON U.IdBillingState = S.IdState
    LEFT JOIN [Country] CO ON S.IdCountry = CO.IdCountry
WHERE
    U.Email = @email",
                    new { email });
                return results.FirstOrDefault();
            }
        }

        public async Task CreateBillingInformation(string accountName, BillingInformation billingInformation)
        {
            using var connection = await _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE [User] SET
    [BillingFirstName] = @firstname,
    [BillingLastName] = @lastname,
    [BillingAddress] = @address,
    [BillingCity] = @city,
    [IdBillingState] = (SELECT IdState FROM [State] WHERE StateCode = @idBillingState),
    [BillingPhone] = @phoneNumber,
    [BillingZip] = @zipCode
WHERE
    Email = @email;",
                new
                {
                    @firstname = billingInformation.Firstname,
                    @lastname = billingInformation.Lastname,
                    @address = billingInformation.Address,
                    @city = billingInformation.City,
                    @idBillingState = billingInformation.Province,
                    @phoneNumber = billingInformation.Phone,
                    @zipCode = billingInformation.ZipCode,
                    @email = accountName
                });
        }

        public async Task UpdateBillingInformation(string accountName, BillingInformation billingInformation)
        {
            using var connection = await _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE [User] SET
    [BillingFirstName] = @firstname,
    [BillingLastName] = @lastname,
    [BillingAddress] = @address,
    [BillingCity] = @city,
    [IdBillingState] = (SELECT IdState FROM [State] WHERE StateCode = @idBillingState),
    [BillingPhone] = @phoneNumber,
    [BillingZip] = @zipCode
WHERE
    Email = @email;",
                new
                {
                    @firstname = billingInformation.Firstname,
                    @lastname = billingInformation.Lastname,
                    @address = billingInformation.Address,
                    @city = billingInformation.City,
                    @idBillingState = billingInformation.Province,
                    @phoneNumber = billingInformation.Phone,
                    @zipCode = billingInformation.ZipCode,
                    @email = accountName
                });
        }

        public async Task<PaymentMethod> GetCurrentPaymentMethod(string username)
        {
            using var connection = await _connectionFactory.GetConnection();

            var result = await connection.QueryFirstOrDefaultAsync<PaymentMethod>(@"

SELECT
    U.CCHolderFullName,
    U.CCNumber,
    U.CCExpMonth,
    U.CCExpYear,
    U.CCVerification,
    C.IdCCType,
    C.[Description] AS CCType,
    P.PaymentMethodName AS PaymentMethodName,
    U.RazonSocial,
    U.IdConsumerType,
    U.CUIT as IdentificationNumber,
    U.ResponsableIVA
FROM
    [User] U
LEFT JOIN
    [CreditCardTypes] C ON C.IdCCType = U.IdCCType
LEFT JOIN
    [PaymentMethods] P ON P.IdPaymentMethod = U.PaymentMethod
WHERE
    U.Email = @email;",
                new
                {
                    @email = username
                });

            result.IdConsumerType = ConsumerTypeHelper.GetConsumerType(result.IdConsumerType);

            if (result is not { PaymentMethodName: "CC" or "MP" })
                return result;

            result.CCHolderFullName = _encryptionService.DecryptAES256(result.CCHolderFullName);
            result.CCNumber = CreditCardHelper.ObfuscateNumber(_encryptionService.DecryptAES256(result.CCNumber));
            result.CCVerification = CreditCardHelper.ObfuscateVerificationCode(_encryptionService.DecryptAES256(result.CCVerification));

            return result;
        }

        public async Task<EmailRecipients> GetInvoiceRecipients(string accountName)
        {
            using var connection = await _connectionFactory.GetConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT
    U.BillingEmails
FROM [User] U
WHERE U.Email = @email;",
                new
                {
                    @email = accountName
                });

            if (user is null) return null;

            return new EmailRecipients
            {
                Recipients = (user.BillingEmails ?? string.Empty).Replace(" ", string.Empty).Split(',')
            };
        }

        public async Task<bool> UpdateCurrentPaymentMethod(string accountName, PaymentMethod paymentMethod)
        {
            using var connection = await _connectionFactory.GetConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT IdUser
FROM [User]
WHERE Email = @email;",
                new
                {
                    @email = accountName
                });

            if (paymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString())
            {
                var creditCard = new CreditCard
                {
                    Number = _encryptionService.EncryptAES256(paymentMethod.CCNumber.Replace(" ", "")),
                    HolderName = _encryptionService.EncryptAES256(paymentMethod.CCHolderFullName),
                    ExpirationMonth = int.Parse(paymentMethod.CCExpMonth),
                    ExpirationYear = int.Parse(paymentMethod.CCExpYear),
                    Code = _encryptionService.EncryptAES256(paymentMethod.CCVerification)
                };

                var cultureInfo = Thread.CurrentThread.CurrentCulture;
                var textInfo = cultureInfo.TextInfo;

                paymentMethod.CCType = textInfo.ToTitleCase(paymentMethod.CCType);

                //Validate CC
                var validCc = Enum.Parse<CardTypeEnum>(paymentMethod.CCType) != CardTypeEnum.Unknown && await _paymentGateway.IsValidCreditCard(creditCard, user.IdUser);
                if (!validCc)
                {
                    return false;
                }

                //Update user payment method in DB
                await UpdateUserPaymentMethod(user, paymentMethod);
            }
            else if (paymentMethod.PaymentMethodName == PaymentMethodEnum.MP.ToString())
            {
                return true;
            }
            else if (paymentMethod.PaymentMethodName == PaymentMethodEnum.TRANSF.ToString())
            {
                await UpdateUserPaymentMethodByTransfer(user, paymentMethod);
            }

            //Send BP to SAP
            await SendUserDataToSap(user, paymentMethod);

            return true;
        }

        private async Task UpdateUserPaymentMethodByTransfer(User user, PaymentMethod paymentMethod)
        {
            using var connection = await _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE
    [USER]
SET
    PaymentMethod = (SELECT IdPaymentMethod FROM [PaymentMethods] WHERE PaymentMethodName = @paymentMethodName),
    RazonSocial = @razonSocial,
    IdConsumerType = (SELECT IdConsumerType FROM [ConsumerTypes] WHERE Name = @idConsumerType),
    IdResponsabileBilling = @idResponsabileBilling,
    CUIT = @cuit,
    ResponsableIVA = @responsableIVA
WHERE
    IdUser = @IdUser;",
                new
                {
                    user.IdUser,
                    @paymentMethodName = paymentMethod.PaymentMethodName,
                    @razonSocial = paymentMethod.RazonSocial,
                    @idConsumerType = paymentMethod.IdConsumerType,
                    @idResponsabileBilling = (int)ResponsabileBillingEnum.GBBISIDE,
                    @cuit = paymentMethod.IdentificationNumber,
                    @responsableIVA = paymentMethod.ResponsableIVA
                });
        }

        private async Task UpdateUserPaymentMethod(User user, PaymentMethod paymentMethod)
        {
            using var connection = await _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE
    [USER]
SET
    CCHolderFullName = @ccHolderFullName,
    CCNumber = @ccNumber,
    CCExpMonth = @ccExpMonth,
    CCExpYear = @ccExpYear,
    CCVerification = @ccVerification,
    IdCCType = @idCCType,
    PaymentMethod = (SELECT IdPaymentMethod FROM [PaymentMethods] WHERE PaymentMethodName = @paymentMethodName),
    RazonSocial = @razonSocial,
    IdConsumerType = (SELECT IdConsumerType FROM [ConsumerTypes] WHERE Name = @idConsumerType),
    IdResponsabileBilling = @idResponsabileBilling
WHERE
    IdUser = @IdUser;",
            new
            {
                user.IdUser,
                @ccHolderFullName = _encryptionService.EncryptAES256(paymentMethod.CCHolderFullName),
                @ccNumber = _encryptionService.EncryptAES256(paymentMethod.CCNumber.Replace(" ", "")),
                @ccExpMonth = paymentMethod.CCExpMonth,
                @ccExpYear = paymentMethod.CCExpYear,
                @ccVerification = _encryptionService.EncryptAES256(paymentMethod.CCVerification),
                @idCCType = Enum.Parse<CardTypeEnum>(paymentMethod.CCType, true),
                @paymentMethodName = paymentMethod.PaymentMethodName,
                @razonSocial = paymentMethod.RazonSocial,
                @idConsumerType = paymentMethod.IdConsumerType,
                @idResponsabileBilling = (int)ResponsabileBillingEnum.QBL
            });
        }

        private async Task SendUserDataToSap(User user, PaymentMethod paymentMethod)
        {
            using var connection = await _connectionFactory.GetConnection();

            var userData = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT
    U.FirstName,
    U.IdUser,
    U.BillingEmails,
    U.RazonSocial,
    U.BillingFirstName,
    U.BillingLastName,
    U.BillingAddress,
    U.CityName,
    U.IdState,
    S.CountryCode as StateCountryCode,
    U.Address,
    U.ZipCode,
    U.BillingZip,
    U.Email,
    U.PhoneNumber,
    U.IdConsumerType,
    U.CUIT,
    U.IsCancelated,
    U.SapProperties,
    U.BlockedAccountNotPayed,
    V.IsInbound as IsInbound,
    BS.CountryCode as BillingStateCountryCode,
    U.PaymentMethod,
    (SELECT IdUserType FROM [UserTypesPlans] WHERE IdUserTypePlan = @idUserTypePlan) as IdUserType,
    IdResponsabileBilling,
    U.IdBillingState,
    BS.Name as BillingStateName,
    U.BillingCity
FROM
    [User] U
LEFT JOIN
    [State] S ON S.IdState = U.IdState
LEFT JOIN
    [Vendor] V ON V.IdVendor = U.IdVendor
LEFT JOIN
    [State] BS ON BS.IdState = U.IdBillingState
WHERE
    U.IdUser = @IdUser;",
                new
                {
                    user.IdUser,
                    @idUserTypePlan = paymentMethod.IdSelectedPlan
                });

            var sapDto = new SapBusinessPartner
            {
                Id = user.IdUser,
                IsClientManager = false
            };

            sapDto.BillingEmails = (userData.BillingEmails ?? string.Empty).Replace(" ", string.Empty).Split(',');
            sapDto.FirstName = SapHelper.GetFirstName(userData);
            sapDto.LastName = string.IsNullOrEmpty(userData.RazonSocial) ? userData.BillingLastName ?? "" : "";
            sapDto.BillingAddress = userData.BillingAddress ?? "";
            sapDto.CityName = userData.CityName ?? "";
            sapDto.StateId = userData.IdState;
            sapDto.CountryCode = userData.StateCountryCode ?? "";
            sapDto.Address = userData.Address ?? "";
            sapDto.ZipCode = userData.ZipCode ?? "";
            sapDto.BillingZip = userData.BillingZip ?? "";
            sapDto.Email = userData.Email;
            sapDto.PhoneNumber = userData.PhoneNumber ?? "";
            sapDto.FederalTaxId = userData.IdConsumerType == (int)ConsumerTypeEnum.CF ? (paymentMethod.IdentificationNumber ?? userData.CUIT) : userData.CUIT;
            sapDto.FederalTaxType = userData.IdConsumerType == (int)ConsumerTypeEnum.CF ? paymentMethod.IdentificationType : sapDto.FederalTaxType;
            sapDto.IdConsumerType = userData.IdConsumerType;
            sapDto.Cancelated = userData.IsCancelated;
            sapDto.SapProperties = JsonConvert.DeserializeObject(userData.SapProperties);
            sapDto.Blocked = userData.BlockedAccountNotPayed;
            sapDto.IsInbound = userData.IsInbound;
            sapDto.BillingCountryCode = userData.BillingStateCountryCode ?? "";
            sapDto.PaymentMethod = (int)userData.PaymentMethod;
            sapDto.PlanType = userData.IdUserType;
            sapDto.BillingSystemId = userData.IdResponsabileBilling;
            sapDto.BillingStateId = ((sapDto.BillingSystemId == (int)ResponsabileBillingEnum.QBL || sapDto.BillingSystemId == (int)ResponsabileBillingEnum.QuickBookUSA) && sapDto.BillingCountryCode != "US") ? string.Empty
                : (sapDto.BillingCountryCode == "US") ? (SapDictionary.StatesDictionary.TryGetValue(userData.IdBillingState, out string stateIdUs) ? stateIdUs : string.Empty)
                : (SapDictionary.StatesDictionary.TryGetValue(userData.IdBillingState, out string stateId) ? stateId : "99");
            sapDto.County = userData.BillingStateName ?? "";
            sapDto.BillingCity = userData.BillingCity ?? "";

            await _sapService.SendUserDataToSap(sapDto);
        }

        #region AGREEMENT

        public async Task<bool> CreateAgreement(string accountName, AgreementInformation agreementInformation)
        {
            var user = await _userRepository.GetUserForBillingCredit(accountName, agreementInformation);
            if (user == null || !agreementInformation.Total.HasValue || user.PaymentMethod != (int)PaymentMethodEnum.CC)
            {
                return false;
            }

            BuyCreditAgreement buyCreditAgreement = new BuyCreditAgreement();
            bool PaymentMade = false;
            try
            {
                var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountName);

                var paymentResult = await CreateCreditCardPaymentAsync(
                    agreementInformation,
                    user.IdUser,
                    encryptedCreditCard);

                if (paymentResult != PaymentResult.Successful && paymentResult != PaymentResult.Pending)
                {
                    return false;
                }

                await CreateCreditCardPaymentAccountingEntryAsync(
                    agreementInformation,
                    user.IdUser,
                    encryptedCreditCard,
                    SourceTypeHelper.SourceTypeEnumMapper(user));

                PaymentMade = true;

                buyCreditAgreement = await BuyCreditAgreementMapperAsync(agreementInformation, user);

                if (paymentResult == PaymentResult.Successful)
                {
                    buyCreditAgreement.BillingCredit.Approved = true;
                    buyCreditAgreement.BillingCredit.Payed = true;
                    var now = DateTime.UtcNow;
                    buyCreditAgreement.BillingCredit.PaymentDate = now;
                    buyCreditAgreement.BillingCredit.ApprovedDate = now;
                }

                var result = await SaveAgreementBuyCreditAsync(buyCreditAgreement, user);
                if (!result)
                    return false;

                //TODO: SEND BILLING TO SAP
                return true;
            }
            catch (DopplerApplicationException e)
            {
                if (PaymentMade)
                {
                    //TODO: ADD GENERATEREFUND
                }

                return false;
            }
        }

        private async Task<BillingCredit> GetBillingCredit(int billingCreditId)
        {
            using var connection = await _connectionFactory.GetConnection();
            var billingCredit = await connection.QueryFirstOrDefaultAsync<BillingCredit>(@"
SELECT
    [IdBillingCredit],
    [Date],
    [IdUser],
    [IdPaymentMethod],
    [PlanFee],
    [PaymentDate],
    [Taxes],
    [IdCurrencyType],
    [CreditsQty],
    [ActivationDate],
    [ExtraEmailFee],
    [TotalCreditsQty],
    [IdBillingCreditType],
    [CCNumber],
    [CCExpMonth],
    [CCExpYear],
    [CCVerification],
    [IdCCType],
    [IdConsumerType],
    [RazonSocial],
    [CUIT],
    [ExclusiveMessage],
    [IdUserTypePlan],
    [DiscountPlanFeePromotion],
    [ExtraCreditsPromotion],
    [SubscribersQty],
    [CCHolderFullName],
    [NroFacturacion],
    [IdDiscountPlan],
    [TotalMonthPlan],
    [CurrentMonthPlan],
    [PaymentType],
    [CFDIUse],
    [PaymentWay],
    [BankName],
    [BankAccount],
    [IdResponsabileBilling],
    [CCIdentificationType],
    [CCIdentificationNumber],
    [ResponsableIVA]
FROM
    [dbo].[BillingCredits]
WHERE
    IdBillingCredit = @billingCreditId",
                new
                {
                    @billingCreditId = billingCreditId
                });

            return billingCredit;
        }

        private async Task<BuyCreditAgreement> BuyCreditAgreementMapperAsync(AgreementInformation agreementInformation, User user)
        {
            var currentPaymentMethod = await GetCurrentPaymentMethod(user.Email);
            BuyCreditAgreement buyCreditAgreement = new BuyCreditAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdCountry
            };

            if (user.PaymentMethod == null)
            {
                buyCreditAgreement.IdPaymentMethod = (int)PaymentMethodEnum.NONE;
            }
            else
            {
                if (user.PaymentMethod.HasValue)
                    buyCreditAgreement.IdPaymentMethod = user.PaymentMethod.Value;

                //credit card properties
                buyCreditAgreement.IdCCType = currentPaymentMethod.IdCCType;
                buyCreditAgreement.CCExpMonth = short.Parse(currentPaymentMethod.CCExpMonth);
                buyCreditAgreement.CCExpYear = short.Parse(currentPaymentMethod.CCExpYear);
                buyCreditAgreement.CCHolderFullName = currentPaymentMethod.CCHolderFullName;
                buyCreditAgreement.CCIdentificationType = currentPaymentMethod.CCType;
                buyCreditAgreement.CCIdentificationNumber = currentPaymentMethod.CCNumber;
                buyCreditAgreement.CCNumber = currentPaymentMethod.CCNumber;
                buyCreditAgreement.CCVerification = currentPaymentMethod.CCVerification;

                buyCreditAgreement.IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ?
                    int.Parse(currentPaymentMethod.IdConsumerType) :
                    null;
                buyCreditAgreement.RazonSocial = currentPaymentMethod.RazonSocial;
                buyCreditAgreement.Cuit = currentPaymentMethod.IdentificationNumber;
                buyCreditAgreement.ResponsableIVA = user.ResponsableIVA;

                if (user.IdCountry == (int)CountryEnum.ARGENTINA && user.PaymentMethod == (int)PaymentMethodEnum.CC)
                {
                    buyCreditAgreement.Cuit = currentPaymentMethod.IdentificationNumber;
                }

                buyCreditAgreement.CFDIUse = user.CFDIUse;
                buyCreditAgreement.PaymentWay = user.PaymentWay;
                buyCreditAgreement.PaymentType = user.PaymentType;
                buyCreditAgreement.BankName = user.BankName;
                buyCreditAgreement.BankAccount = user.BankAccount;
            }

            buyCreditAgreement.BillingCredit = CreateBillingCredit(agreementInformation, user);

            return buyCreditAgreement;
        }

        private BillingCreditModel CreateBillingCredit(AgreementInformation agreementInformation, User user)
        {
            BillingCreditModel billingCredit = new BillingCreditModel();
            billingCredit.Date = DateTime.UtcNow;
            billingCredit.IdUserTypePlan = agreementInformation.PlanId;
            billingCredit.IdDiscountPlan = agreementInformation.DiscountId;
            billingCredit.IdPaymentMethod = user.PaymentMethod;

            if (agreementInformation.Total.HasValue)
            {
                billingCredit.PlanFee = agreementInformation.Total.Value;
            }

            if (user.NewUserTypePlan.EmailQty.HasValue)
            {
                billingCredit.CreditsQty = user.NewUserTypePlan.EmailQty.Value;
            }

            if (user.NewUserTypePlan.SubscribersQty.HasValue)
            {
                billingCredit.CreditsQty = user.NewUserTypePlan.SubscribersQty.Value;
            }

            if (user.NewUserTypePlan.ExtraEmailCost.HasValue)
            {
                billingCredit.ExtraEmailFee = user.NewUserTypePlan.ExtraEmailCost.Value;
            }

            if (!agreementInformation.Total.HasValue && user.PaymentMethod != (int)PaymentMethodEnum.CC)
            {
                billingCredit.Taxes = (billingCredit.PlanFee - (billingCredit.PlanFee * billingCredit.DiscountPlanFeePromotion / 100)) * 21 / 100;
            }

            return billingCredit;
        }

        private async Task<bool> SaveAgreementBuyCreditAsync(BuyCreditAgreement dtoBuyCredits, User user)
        {
            if (dtoBuyCredits == null)
                return false;

            var billingCreditId = 0;
            switch (dtoBuyCredits.IdPaymentMethod)
            {
                case (int)PaymentMethodEnum.CC:
                    billingCreditId = await CreateBillingCreditForCreditCardPaymentMethod(dtoBuyCredits, user);
                    if (billingCreditId > 0)
                    {
                        user.IdCurrentBillingCredit = billingCreditId;
                    }

                    if (user.UTCFirstPayment == null)
                    {
                        user.UTCFirstPayment = dtoBuyCredits.BillingCredit.PaymentDate.Value.ToShortDateString();
                    }

                    break;
                default:
                    return false;
            }

            if (dtoBuyCredits.Cuit != null)
                user.CUIT = dtoBuyCredits.Cuit;

            var updateResult = await _userRepository.UpdateUserBillingCredit(user);
            if (updateResult == 0)
            {
                throw new DopplerApplicationException(ApplicationErrorCode.CreateAgreementUserUpdateError);
            }

            if (dtoBuyCredits.BillingCredit.PlanFee.HasValue)
            {
                if (dtoBuyCredits.IdPaymentMethod == (int)PaymentMethodEnum.CC)
                {
                    await CreateMovementCreditAsync(billingCreditId, await _userRepository.GetAvailableCredit(dtoBuyCredits.IdUser), user);
                }
            }

            //TODO: SEND NOTIFICATIONS
            return true;
        }

        private async Task<int> CreateBillingCreditForCreditCardPaymentMethod(BuyCreditAgreement dtoBuyCredits, User user)
        {
            var utcNow = DateTime.UtcNow;
            var connection = await _connectionFactory.GetConnection();

            var result = await connection.QueryAsync<int>(@"
INSERT INTO [dbo].[BillingCredits]
    ([Date],
    [IdUser],
    [IdPaymentMethod],
    [PlanFee],
    [PaymentDate],
    [Taxes],
    [IdCurrencyType],
    [CreditsQty],
    [ActivationDate],
    [ExtraEmailFee],
    [TotalCreditsQty],
    [IdBillingCreditType],
    [CCNumber],
    [CCExpMonth],
    [CCExpYear],
    [CCVerification],
    [IdCCType],
    [IdConsumerType],
    [RazonSocial],
    [CUIT],
    [ExclusiveMessage],
    [IdUserTypePlan],
    [DiscountPlanFeePromotion],
    [ExtraCreditsPromotion],
    [SubscribersQty],
    [CCHolderFullName],
    [NroFacturacion],
    [IdDiscountPlan],
    [TotalMonthPlan],
    [CurrentMonthPlan],
    [PaymentType],
    [CFDIUse],
    [PaymentWay],
    [BankName],
    [BankAccount],
    [IdResponsabileBilling],
    [CCIdentificationType],
    [CCIdentificationNumber],
    [ResponsableIVA])
VALUES (
    @date,
    @idUser,
    @idPaymentMethod,
    @planFee,
    @paymentDate,
    @taxes,
    @idCurrencyType,
    @creditsQty,
    @activationDate,
    @extraEmailFee,
    @totalCreditsQty,
    @idBillingCreditType,
    @ccNumber,
    @ccExpMonth,
    @ccExpYear,
    @ccVerification,
    @idCCType,
    @idConsumerType,
    @razonSocial,
    @cuit,
    @exclusiveMessage,
    @idUserTypePlan,
    @discountPlanFeePromotion,
    @extraCreditsPromotion,
    @subscribersQty,
    @ccHolderFullName,
    @nroFacturacion,
    @idDiscountPlan,
    @totalMonthPlan,
    @currentMonthPlan,
    @paymentType,
    @cfdiUse,
    @paymentWay,
    @bankName,
    @bankAccount,
    @idResponsabileBilling,
    @ccIdentificationType,
    @ccIdentificationNumber,
    @responsableIVA);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @date = utcNow,
                @idUser = dtoBuyCredits.IdUser,
                @idPaymentMethod = dtoBuyCredits.IdPaymentMethod,
                @planFee = dtoBuyCredits.BillingCredit.PlanFee,
                @paymentDate = utcNow,
                @taxes = dtoBuyCredits.BillingCredit.Taxes,
                @idCurrencyType = (int)CurrencyTypeIsoEnum.USD,
                @creditsQty = dtoBuyCredits.BillingCredit.CreditsQty,
                @activationDate = utcNow,
                @extraEmailFee = dtoBuyCredits.BillingCredit.ExtraEmailFee,
                @totalCreditsQty = dtoBuyCredits.BillingCredit.CreditsQty,
                @idBillingCreditType = BillingCreditTypeHelper.BillingCreditTypeEnumMapper(user),
                @ccNumber = _encryptionService.EncryptAES256(dtoBuyCredits.CCNumber),
                @ccExpMonth = dtoBuyCredits.CCExpMonth,
                @ccExpYear = dtoBuyCredits.CCExpYear,
                @ccVerification = _encryptionService.EncryptAES256(dtoBuyCredits.CCVerification),
                @idCCType = dtoBuyCredits.IdCCType,
                @idConsumerType = (dtoBuyCredits.IdPaymentMethod == (int)PaymentMethodEnum.MP && !dtoBuyCredits.IdConsumerType.HasValue) ?
                    (int)ConsumerTypeEnum.CF :
                    dtoBuyCredits.IdConsumerType,
                @razonSocial = dtoBuyCredits.RazonSocial,
                @cuit = dtoBuyCredits.Cuit ?? dtoBuyCredits.Rfc,
                @exclusiveMessage = dtoBuyCredits.ExclusiveMessage,
                @idUserTypePlan = dtoBuyCredits.BillingCredit.IdUserTypePlan,
                @discountPlanFeePromotion = dtoBuyCredits.BillingCredit.DiscountPlanFeePromotion,
                @extraCreditsPromotion = dtoBuyCredits.BillingCredit.ExtraCreditsPromotion,
                @subscribersQty = dtoBuyCredits.BillingCredit.SubscribersQty,
                @ccHolderFullName = _encryptionService.EncryptAES256(dtoBuyCredits.CCHolderFullName),
                @nroFacturacion = 0,
                @idDiscountPlan = dtoBuyCredits.BillingCredit.IdDiscountPlan,
                @totalMonthPlan = dtoBuyCredits.BillingCredit.MonthPlan,
                @currentMonthPlan = dtoBuyCredits.BillingCredit.MonthPlan,
                @paymentType = dtoBuyCredits.PaymentType,
                @cfdiUse = dtoBuyCredits.CFDIUse,
                @paymentWay = dtoBuyCredits.PaymentWay,
                @bankName = dtoBuyCredits.BankName,
                @bankAccount = dtoBuyCredits.BankAccount,
                @idResponsabileBilling = (int)ResponsabileBillingEnum.QBL,
                @ccIdentificationType = dtoBuyCredits.CCIdentificationType,
                @ccIdentificationNumber = dtoBuyCredits.CCIdentificationNumber,
                @responsableIVA = dtoBuyCredits.ResponsableIVA
            });

            return result.FirstOrDefault();
        }

        private async Task<int> CreateMovementCreditAsync(int idBillingCredit, int partialBalance, User user)
        {
            BillingCredit billingCredit = await GetBillingCredit(idBillingCredit);

            if (billingCredit == null)
                throw new DopplerApplicationException(ApplicationErrorCode.InexistentBillingCredit);

            if (billingCredit.IdUser == 0)
                throw new DopplerApplicationException(ApplicationErrorCode.InexistentUser);

            if (billingCredit.IdUserTypePlan == null)
                throw new DopplerApplicationException(ApplicationErrorCode.InvalidBilling);

            if (billingCredit.TotalCreditsQty == null)
                throw new DopplerApplicationException(ApplicationErrorCode.InvalidBilling);

            MovementCredit movementsCredit = new MovementCredit();

            movementsCredit.IdUserType = user.NewUserTypePlan.IdUserType;
            movementsCredit.CreditsQty = billingCredit.TotalCreditsQty.Value;
            movementsCredit.PartialBalance = partialBalance + billingCredit.TotalCreditsQty.Value;

            if (billingCredit.ActivationDate.HasValue)
                movementsCredit.Date = billingCredit.ActivationDate.Value;
            else
                movementsCredit.Date = DateTime.UtcNow;

            movementsCredit.IdUser = billingCredit.IdUser;
            movementsCredit.IdBillingCredit = billingCredit.IdBillingCredit;

            if (movementsCredit.IdUserType == (int)UserTypeEnum.INDIVIDUAL)
            {
                movementsCredit.ConceptEnglish = "Credits Acreditation";
                movementsCredit.ConceptSpanish = "Acreditación de Créditos";
            }
            else
            {
                string month = movementsCredit.Date.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"));
                month = char.ToUpper(month[0]) + month.Substring(1);
                movementsCredit.ConceptSpanish = "Acreditación de Emails Mes: " + month;
                movementsCredit.ConceptEnglish = "Monthly Emails Accreditation: " + movementsCredit.Date.ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
            }

            var connection = await _connectionFactory.GetConnection();

            var result = await connection.QueryAsync<int>(@"
INSERT INTO [dbo].[MovementsCredits]
    ([IdUser],
    [Date],
    [CreditsQty],
    [IdBillingCredit],
    [PartialBalance],
    [ConceptEnglish],
    [ConceptSpanish],
    [IdUserType])
VALUES
    (@idUser,
    @date,
    @creditsQty,
    @idBillingCredit,
    @partialBalance,
    @conceptEnglish,
    @conceptSpanish,
    @idUserType);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idUser = movementsCredit.IdUser,
                @date = movementsCredit.Date,
                @idUserType = movementsCredit.IdUserType,
                @creditsQty = movementsCredit.CreditsQty,
                @idBillingCredit = movementsCredit.IdBillingCredit,
                @partialBalance = movementsCredit.PartialBalance,
                @conceptEnglish = movementsCredit.ConceptEnglish,
                @conceptSpanish = movementsCredit.ConceptSpanish
            });

            return result.FirstOrDefault();
        }

        private async Task<int> CreateInvoiceEntryAsync(InvoiceEntry invoiceEntry)
        {
            var connection = await _connectionFactory.GetConnection();

            var result = await connection.QueryAsync<int>(@"
INSERT INTO [dbo].[AccountingEntry]
    ([Date],
    [Amount],
    [Status],
    [Source],
    [AuthorizationNumber],
    [InvoiceNumber],
    [AccountEntryType],
    [AccountingTypeDescription],
    [IdClient],
    [IdAccountType],
    [IdInvoiceBillingType])
VALUES
    (@date,
    @amount,
    @status,
    @source,
    @authorizationNumber,
    @invoiceNumber,
    @accountEntryType,
    @accountingTypeDescription,
    @idClient,
    @idAccountType,
    @idInvoiceBillingType);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idClient = invoiceEntry.IdClient,
                @amount = invoiceEntry.Amount,
                @date = invoiceEntry.Date,
                @status = invoiceEntry.Status,
                @source = invoiceEntry.Source,
                @accountingTypeDescription = invoiceEntry.AccountingTypeDescription,
                @invoiceNumber = invoiceEntry.InvoiceNumber,
                @idAccountType = invoiceEntry.IdAccountType,
                @idInvoiceBillingType = invoiceEntry.IdInvoiceBillingType,
                @authorizationNumber = invoiceEntry.AuthorizationNumber,
                @accountEntryType = invoiceEntry.AccountEntryType
            });

            return result.FirstOrDefault();
        }

        private async Task<int> CreateCreditCardPaymentEntryAsync(CreditCardPaymentEntry creditCardPaymentEntry)
        {
            var connection = await _connectionFactory.GetConnection();

            var result = await connection.QueryAsync<int>(@"
INSERT INTO [dbo].[AccountingEntry]
    ([IdClient],
    [IdInvoice],
    [Amount],
    [CCNumber],
    [CCExpMonth],
    [CCExpYear],
    [CCHolderName],
    [Date],
    [Status],
    [Source],
    [AccountingTypeDescription],
    [IdAccountType],
    [IdInvoiceBillingType],
    [AccountEntryType],
    [AuthorizationNumber],
    [PaymentEntryType])
VALUES
    (@idClient,
    @idInvoice,
    @amount,
    @ccCNumber,
    @ccExpMonth,
    @ccExpYear,
    @ccHolderName,
    @date,
    @status,
    @source,
    @accountingTypeDescription,
    @idAccountType,
    @idInvoiceBillingType,
    @accountEntryType,
    @authorizationNumber,
    @paymentEntryType);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idClient = creditCardPaymentEntry.IdClient,
                @idInvoice = creditCardPaymentEntry.IdInvoice,
                @amount = creditCardPaymentEntry.Amount,
                @ccCNumber = creditCardPaymentEntry.CCNumber,
                @ccExpMonth = creditCardPaymentEntry.CCExpMonth,
                @ccExpYear = creditCardPaymentEntry.CCExpYear,
                @ccHolderName = creditCardPaymentEntry.CCHolderName,
                @date = creditCardPaymentEntry.Date,
                @status = creditCardPaymentEntry.Status,
                @source = creditCardPaymentEntry.Source,
                @accountingTypeDescription = creditCardPaymentEntry.AccountingTypeDescription,
                @idAccountType = creditCardPaymentEntry.IdAccountType,
                @idInvoiceBillingType = creditCardPaymentEntry.IdInvoiceBillingType,
                @accountEntryType = creditCardPaymentEntry.AccountEntryType,
                @authorizationNumber = creditCardPaymentEntry.AuthorizationNumber,
                @paymentEntryType = creditCardPaymentEntry.PaymentEntryType
            });

            return result.FirstOrDefault();
        }

        private async Task<PaymentResult> CreateCreditCardPaymentAsync(AgreementInformation agreementInformation, int userId, CreditCard encryptedCreditCard)
        {
            try
            {
                //make payment
                if (agreementInformation.Total != 0)
                {
                    agreementInformation.AuthorizationNumber = await _paymentGateway.CreateCreditCardPayment(
                        encryptedCreditCard,
                        agreementInformation.Total,
                        userId);
                }
            }
            catch (Exception)
            {
                return PaymentResult.Failure;
            }

            return PaymentResult.Successful;
        }

        private async Task<PaymentResult> CreateCreditCardPaymentAccountingEntryAsync(AgreementInformation agreementInformation, int userId, CreditCard encryptedCreditCard, int sourceTypeId)
        {
            var invoice = new InvoiceEntry
            {
                IdClient = userId,
                Amount = (decimal)agreementInformation.Total,
                Date = DateTime.UtcNow,
                Status = ApprovedAccountingEntryStatus,
                Source = sourceTypeId,
                AccountingTypeDescription = InvoiceAccountingEntryTypeDescription,
                InvoiceNumber = 0,
                IdAccountType = UserAccountType,
                IdInvoiceBillingType = (int)InvoiceBillingTypeEnum.QBL,
                AuthorizationNumber = agreementInformation.AuthorizationNumber,
                AccountEntryType = InvoiceAccountEntryType
            };

            var invoiceId = await CreateInvoiceEntryAsync(invoice);

            var ccPaymentEntry = new CreditCardPaymentEntry
            {
                IdClient = invoice.IdClient,
                Amount = (decimal)agreementInformation.Total,
                IdInvoice = invoiceId,
                CCNumber = encryptedCreditCard.Number,
                CCExpMonth = (short?)encryptedCreditCard.ExpirationMonth,
                CCExpYear = (short?)encryptedCreditCard.ExpirationYear,
                CCHolderName = encryptedCreditCard.HolderName,
                Date = DateTime.UtcNow,
                Source = sourceTypeId,
                AuthorizationNumber = agreementInformation.AuthorizationNumber,
                AccountingTypeDescription = CCPaymentAccountingEntryTypeDescription,
                IdAccountType = UserAccountType,
                IdInvoiceBillingType = invoice.IdInvoiceBillingType,
                AccountEntryType = PaymentAccountEntryType,
                PaymentEntryType = PaymentPaymentEntryType
            };

            await CreateCreditCardPaymentEntryAsync(ccPaymentEntry);

            agreementInformation.InvoiceNumber = invoiceId != 0 ? invoice.IdAccountingEntry : 0;
            agreementInformation.TransferReference = invoice != null ? invoice.AuthorizationNumber : string.Empty;

            return PaymentResult.Successful;
        }

        #endregion AGREEMENT
    }
}
