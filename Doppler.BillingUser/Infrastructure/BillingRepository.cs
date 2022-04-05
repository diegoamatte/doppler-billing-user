using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
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

        private const int InvoiceBillingTypeQBL = 1;
        private const int UserAccountType = 1;
        private const string AccountingEntryStatusApproved = "Approved";
        private const string AccountingEntryTypeDescriptionInvoice = "Invoice";
        private const string AccountingEntryTypeDescriptionCCPayment = "CC Payment";
        private const string AccountEntryTypeInvoice = "I";
        private const string AccountEntryTypePayment = "P";
        private const string PaymentEntryTypePayment = "P";
        private const int CurrencyTypeUsd = 0;
        private const int BillingCreditTypeUpgradeRequest = 1;

        public BillingRepository(IDatabaseConnectionFactory connectionFactory,
            IEncryptionService encryptionService,
            IPaymentGateway paymentGateway,
            ISapService sapService)
        {
            _connectionFactory = connectionFactory;
            _encryptionService = encryptionService;
            _paymentGateway = paymentGateway;
            _sapService = sapService;
        }
        public async Task<BillingInformation> GetBillingInformation(string email)
        {
            using (IDbConnection connection = _connectionFactory.GetConnection())
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

        public async Task UpdateBillingInformation(string accountName, BillingInformation billingInformation)
        {
            using var connection = _connectionFactory.GetConnection();

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
            using var connection = _connectionFactory.GetConnection();

            var result = await connection.QueryFirstOrDefaultAsync<PaymentMethod>(@"

SELECT
    U.CCHolderFullName,
    U.CCNumber,
    U.CCExpMonth,
    U.CCExpYear,
    U.CCVerification,
    C.[Description] AS CCType,
    P.PaymentMethodName AS PaymentMethodName,
    U.RazonSocial,
    U.IdConsumerType,
    U.CUIT as IdentificationNumber,
    U.ResponsableIVA,
    U.IdCCType
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
            using var connection = _connectionFactory.GetConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT
    U.BillingEmails
FROM
    [User] U
WHERE
    U.Email = @email;",
                new
                {
                    @email = accountName
                });

            if (user is null) return null;

            return new EmailRecipients
            {
                Recipients = string.IsNullOrEmpty(user.BillingEmails) ? Array.Empty<string>() : user.BillingEmails.Replace(" ", string.Empty).Split(',')
            };
        }

        public async Task UpdateInvoiceRecipients(string accountName, string[] emailRecipients, int planId)
        {
            using var connection = _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE
    [User]
SET
    BillingEmails = @emailRecipients
WHERE
    Email = @email;",
                new
                {
                    @email = accountName,
                    @emailRecipients = string.Join(",", emailRecipients)
                });

            await SendUserDataToSap(accountName, planId);
        }

        public async Task<CurrentPlan> GetCurrentPlan(string accountName)
        {
            using var connection = _connectionFactory.GetConnection();

            var currentPlan = await connection.QueryFirstOrDefaultAsync<CurrentPlan>(@"
SELECT
    B.IdUserTypePlan as IdPlan,
    D.MonthPlan AS PlanSubscription,
    UT.Description AS PlanType,
    T.EmailQty,
    T.SubscribersQty,
    (CASE WHEN T.IdUserType != 4 THEN PartialBalance.Total ELSE T.SubscribersQty - ISNULL(Subscribers.Total, 0) END) AS RemainingCredits
FROM
    [BillingCredits] B
LEFT JOIN
    [UserTypesPlans] T ON T.[IdUserTypePlan] = B.[IdUserTypePlan]
LEFT JOIN
    [DiscountXPlan] D ON D.[IdDiscountPlan] = B.[IdDiscountPlan]
LEFT JOIN
    [UserTypes] UT ON UT.[IdUserType] = T.[IdUserType]
OUTER APPLY (SELECT TOP 1 MC.[PartialBalance] AS Total
    FROM [dbo].[MovementsCredits] MC
    WHERE MC.IdUser = (SELECT IdUser FROM [User] WHERE Email = @email)
    ORDER BY MC.[IdMovementCredit] DESC) PartialBalance
OUTER APPLY (
    SELECT SUM(VSBSXUA.Amount) AS Total
    FROM [dbo].[ViewSubscribersByStatusXUserAmount] VSBSXUA WITH (NOEXPAND)
    WHERE VSBSXUA.IdUser = (SELECT IdUser FROM [User] WHERE Email = @email)) Subscribers
WHERE
    B.[IdUser] = (SELECT IdUser FROM [User] WHERE Email = @email) ORDER BY B.[Date] DESC;",
                new
                {
                    @email = accountName
                });

            return currentPlan;
        }

        public async Task<bool> UpdateCurrentPaymentMethod(User user, PaymentMethod paymentMethod)
        {
            using var connection = _connectionFactory.GetConnection();

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
            if (paymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString())
            {
                await SendUserDataToSap(user.Email, paymentMethod.IdSelectedPlan);
            }

            return true;
        }

        private async Task UpdateUserPaymentMethodByTransfer(User user, PaymentMethod paymentMethod)
        {
            using var connection = _connectionFactory.GetConnection();

            await connection.ExecuteAsync(@"
UPDATE
    [USER]
SET
    PaymentMethod = (SELECT IdPaymentMethod FROM [PaymentMethods] WHERE PaymentMethodName = @paymentMethodName),
    RazonSocial = @razonSocial,
    IdConsumerType = (SELECT IdConsumerType FROM [ConsumerTypes] WHERE Name = @idConsumerType),
    IdResponsabileBilling = @idResponsabileBilling,
    CUIT = @cuit,
    ResponsableIVA = @responsableIVA,
    CFDIUse = @useCFDI,
    PaymentType = @paymentType,
    PaymentWay = @paymentWay,
    BankName = @bankAccount,
    BankAccount = @bankName
WHERE
    IdUser = @IdUser;",
                new
                {
                    user.IdUser,
                    @paymentMethodName = paymentMethod.PaymentMethodName,
                    @razonSocial = paymentMethod.RazonSocial,
                    @idConsumerType = paymentMethod.IdConsumerType,
                    @idResponsabileBilling = user.IdBillingCountry == (int)CountryEnum.Colombia ? (int)ResponsabileBillingEnum.BorisMarketing : (int)ResponsabileBillingEnum.RC,
                    @cuit = paymentMethod.IdentificationNumber,
                    @responsableIVA = paymentMethod.ResponsableIVA,
                    @useCFDI = user.IdBillingCountry == (int)CountryEnum.Mexico ? paymentMethod.UseCFDI : null,
                    @paymentType = user.IdBillingCountry == (int)CountryEnum.Mexico ? paymentMethod.PaymentType : null,
                    @paymentWay = user.IdBillingCountry == (int)CountryEnum.Mexico ? paymentMethod.PaymentWay.ToString() : null,
                    @bankAccount = user.IdBillingCountry == (int)CountryEnum.Mexico && paymentMethod.PaymentWay == PaymentWayEnum.TRANSFER.ToString() ? paymentMethod.BankAccount : null,
                    @bankName = user.IdBillingCountry == (int)CountryEnum.Mexico && paymentMethod.PaymentWay == PaymentWayEnum.TRANSFER.ToString() ? paymentMethod.BankName : null,
                });
        }

        private async Task UpdateUserPaymentMethod(User user, PaymentMethod paymentMethod)
        {
            using var connection = _connectionFactory.GetConnection();

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

        private async Task SendUserDataToSap(string accountName, int planId)
        {
            using var connection = _connectionFactory.GetConnection();

            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
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
    U.IdResponsabileBilling,
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
    U.Email = @accountName;",
                new
                {
                    accountName,
                    @idUserTypePlan = planId
                });

            if (user.IdResponsabileBilling is (int)ResponsabileBillingEnum.QBL or (int)ResponsabileBillingEnum.GBBISIDE)
            {
                var sapDto = new SapBusinessPartner
                {
                    Id = user.IdUser,
                    IsClientManager = false,
                    BillingEmails = (user.BillingEmails ?? string.Empty).Replace(" ", string.Empty).Split(','),
                    FirstName = SapHelper.GetFirstName(user),
                    LastName = string.IsNullOrEmpty(user.RazonSocial) ? user.BillingLastName ?? "" : "",
                    BillingAddress = user.BillingAddress ?? "",
                    CityName = user.CityName ?? "",
                    StateId = user.IdState,
                    CountryCode = user.StateCountryCode ?? "",
                    Address = user.Address ?? "",
                    ZipCode = user.ZipCode ?? "",
                    BillingZip = user.BillingZip ?? "",
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "",
                    FederalTaxId = user.CUIT,
                    IdConsumerType = user.IdConsumerType,
                    Cancelated = user.IsCancelated,
                    SapProperties = JsonConvert.DeserializeObject(user.SapProperties),
                    Blocked = user.BlockedAccountNotPayed,
                    IsInbound = user.IsInbound,
                    BillingCountryCode = user.BillingStateCountryCode ?? "",
                    PaymentMethod = user.PaymentMethod,
                    PlanType = user.IdUserType,
                    BillingSystemId = user.IdResponsabileBilling
                };

                sapDto.BillingStateId = ((sapDto.BillingSystemId == (int)ResponsabileBillingEnum.QBL || sapDto.BillingSystemId == (int)ResponsabileBillingEnum.QuickBookUSA) && sapDto.BillingCountryCode != "US") ? string.Empty
                    : (sapDto.BillingCountryCode == "US") ? (SapDictionary.StatesDictionary.TryGetValue(user.IdBillingState, out string stateIdUs) ? stateIdUs : string.Empty)
                    : (SapDictionary.StatesDictionary.TryGetValue(user.IdBillingState, out string stateId) ? stateId : "99");
                sapDto.County = user.BillingStateName ?? "";
                sapDto.BillingCity = user.BillingCity ?? "";

                await _sapService.SendUserDataToSap(sapDto);
            }
        }

        private async Task<PaymentMethod> GetPaymentMethodByUserName(string username)
        {
            using var connection = _connectionFactory.GetConnection();

            var result = await connection.QueryFirstOrDefaultAsync<PaymentMethod>(@"

SELECT
    U.CCHolderFullName,
    U.CCNumber,
    U.CCExpMonth,
    U.CCExpYear,
    U.CCVerification,
    C.[Description] AS CCType,
    P.PaymentMethodName AS PaymentMethodName,
    U.RazonSocial,
    U.IdConsumerType,
    U.CUIT as IdentificationNumber,
    U.ResponsableIVA,
    U.IdCCType
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

            return result;
        }

        public async Task<int> CreateAccountingEntriesAsync(AgreementInformation agreementInformation, CreditCard encryptedCreditCard, int userId, UserTypePlanInformation newPlan, string authorizationNumber)
        {
            var connection = _connectionFactory.GetConnection();
            var invoiceId = await connection.QueryFirstOrDefaultAsync<int>(@"
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
                @idClient = userId,
                @amount = agreementInformation.Total,
                @date = DateTime.UtcNow,
                @status = AccountingEntryStatusApproved,
                @source = SourceTypeHelper.SourceTypeEnumMapper(newPlan),
                @accountingTypeDescription = AccountingEntryTypeDescriptionInvoice,
                @invoiceNumber = 0,
                @idAccountType = UserAccountType,
                @idInvoiceBillingType = InvoiceBillingTypeQBL,
                @authorizationNumber = authorizationNumber,
                @accountEntryType = AccountEntryTypeInvoice
            });

            await connection.QueryFirstOrDefaultAsync<int>(@"
INSERT INTO [dbo].[AccountingEntry]
    ([IdClient],
    [IdInvoice],
    [Amount],
    [CCNumber],
    [CCExpMonth],
    [CCExpYear],
    [CCHolderName],
    [Date],
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
                @idClient = userId,
                @idInvoice = invoiceId,
                @amount = agreementInformation.Total,
                @ccCNumber = encryptedCreditCard.Number,
                @ccExpMonth = encryptedCreditCard.ExpirationMonth,
                @ccExpYear = encryptedCreditCard.ExpirationYear,
                @ccHolderName = encryptedCreditCard.HolderName,
                @date = DateTime.UtcNow,
                @source = SourceTypeHelper.SourceTypeEnumMapper(newPlan),
                @accountingTypeDescription = AccountingEntryTypeDescriptionCCPayment,
                @idAccountType = UserAccountType,
                @idInvoiceBillingType = InvoiceBillingTypeQBL,
                @accountEntryType = AccountEntryTypePayment,
                @authorizationNumber = authorizationNumber,
                @paymentEntryType = PaymentEntryTypePayment
            });

            return invoiceId;
        }

        public async Task<int> CreateBillingCreditAsync(
            AgreementInformation agreementInformation,
            UserBillingInformation user,
            UserTypePlanInformation newUserTypePlan,
            Promotion promotion)
        {
            var currentPaymentMethod = await GetPaymentMethodByUserName(user.Email);

            var buyCreditAgreement = new CreateAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdBillingCountry,
                IdPaymentMethod = (int)user.PaymentMethod,
                IdCCType = currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? currentPaymentMethod.IdCCType : null,
                CCExpMonth = currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? short.Parse(currentPaymentMethod.CCExpMonth) : null,
                CCExpYear = currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? short.Parse(currentPaymentMethod.CCExpYear) : null,
                CCHolderFullName = currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? currentPaymentMethod.CCHolderFullName : null,
                CCIdentificationType = currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? currentPaymentMethod.CCType : null,
                CCIdentificationNumber = currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? currentPaymentMethod.CCNumber : null,
                CCNumber = currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? currentPaymentMethod.CCNumber : null,
                CCVerification = currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? currentPaymentMethod.CCVerification : null,
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType) : null,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = user.IdBillingCountry == (int)CountryEnum.Colombia ? currentPaymentMethod.IdentificationNumber : null,
                CFDIUse = user.CFDIUse,
                PaymentWay = user.PaymentWay,
                PaymentType = user.PaymentType,
                BankName = user.BankName,
                BankAccount = user.BankAccount
            };

            DateTime now = DateTime.UtcNow;
            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion) ? now : null,
                ActivationDate = user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion) ? now : null,
                Approved = user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion),
                Payed = user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion),
                IdUserTypePlan = newUserTypePlan.IdUserTypePlan,
                PlanFee = newUserTypePlan.Fee,
                CreditsQty = newUserTypePlan.EmailQty ?? null,
                ExtraEmailFee = newUserTypePlan.ExtraEmailCost ?? null,
                ExtraCreditsPromotion = promotion?.ExtraCredits
            };

            if (newUserTypePlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
            {
                var planDiscountInformation = await GetPlanDiscountInformation(agreementInformation.DiscountId);

                buyCreditAgreement.BillingCredit.IdDiscountPlan = agreementInformation.DiscountId != 0 ? agreementInformation.DiscountId : null;
                buyCreditAgreement.BillingCredit.TotalMonthPlan = planDiscountInformation?.MonthPlan;
                buyCreditAgreement.BillingCredit.CurrentMonthPlan =
                    (buyCreditAgreement.BillingCredit.TotalMonthPlan.HasValue
                    && buyCreditAgreement.BillingCredit.TotalMonthPlan.Value > 1
                    && buyCreditAgreement.BillingCredit.Date.Day > 20)
                    ? 0 : 1;
                buyCreditAgreement.BillingCredit.SubscribersQty = newUserTypePlan.SubscribersQty;
            }

            var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryFirstOrDefaultAsync<int>(@"
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
    [ResponsableIVA],
    [IdPromotion])
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
    @responsableIVA,
    @idPromotion);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @date = now,
                @idUser = buyCreditAgreement.IdUser,
                @idPaymentMethod = buyCreditAgreement.IdPaymentMethod,
                @planFee = buyCreditAgreement.BillingCredit.PlanFee,
                @paymentDate = buyCreditAgreement.BillingCredit.PaymentDate,
                @taxes = buyCreditAgreement.BillingCredit.Taxes,
                @idCurrencyType = CurrencyTypeUsd,
                @creditsQty = buyCreditAgreement.BillingCredit.CreditsQty,
                @activationDate = buyCreditAgreement.BillingCredit.ActivationDate,
                @extraEmailFee = buyCreditAgreement.BillingCredit.ExtraEmailFee,
                @totalCreditsQty = buyCreditAgreement.BillingCredit.CreditsQty + (buyCreditAgreement.BillingCredit.ExtraCreditsPromotion ?? 0),
                @idBillingCreditType = BillingCreditTypeUpgradeRequest,
                @ccNumber = buyCreditAgreement.CCNumber,
                @ccExpMonth = buyCreditAgreement.CCExpMonth,
                @ccExpYear = buyCreditAgreement.CCExpYear,
                @ccVerification = buyCreditAgreement.CCVerification,
                @idCCType = buyCreditAgreement.IdCCType,
                @idConsumerType = (buyCreditAgreement.IdPaymentMethod == (int)PaymentMethodEnum.MP && !buyCreditAgreement.IdConsumerType.HasValue) ?
                    (int)ConsumerTypeEnum.CF :
                    buyCreditAgreement.IdConsumerType,
                @razonSocial = buyCreditAgreement.RazonSocial,
                @cuit = buyCreditAgreement.Cuit ?? buyCreditAgreement.Rfc,
                @exclusiveMessage = buyCreditAgreement.ExclusiveMessage,
                @idUserTypePlan = buyCreditAgreement.BillingCredit.IdUserTypePlan,
                @discountPlanFeePromotion = buyCreditAgreement.BillingCredit.DiscountPlanFeePromotion,
                @extraCreditsPromotion = buyCreditAgreement.BillingCredit.ExtraCreditsPromotion,
                @subscribersQty = buyCreditAgreement.BillingCredit.SubscribersQty,
                @ccHolderFullName = buyCreditAgreement.CCHolderFullName,
                @nroFacturacion = 0,
                @idDiscountPlan = buyCreditAgreement.BillingCredit.IdDiscountPlan,
                @totalMonthPlan = buyCreditAgreement.BillingCredit.TotalMonthPlan,
                @currentMonthPlan = buyCreditAgreement.BillingCredit.CurrentMonthPlan,
                @paymentType = buyCreditAgreement.PaymentType,
                @cfdiUse = buyCreditAgreement.CFDIUse,
                @paymentWay = buyCreditAgreement.PaymentWay,
                @bankName = buyCreditAgreement.BankName,
                @bankAccount = buyCreditAgreement.BankAccount,
                @idResponsabileBilling =
                    (user.PaymentMethod == PaymentMethodEnum.TRANSF && user.IdBillingCountry == (int)CountryEnum.Colombia) ?
                    (int)ResponsabileBillingEnum.BorisMarketing :
                    (int)ResponsabileBillingEnum.QBL,
                @ccIdentificationType = buyCreditAgreement.CCIdentificationType,
                @ccIdentificationNumber = currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ?
                    CreditCardHelper.ObfuscateNumber(_encryptionService.DecryptAES256(buyCreditAgreement.CCNumber)) :
                    null,
                @responsableIVA = buyCreditAgreement.ResponsableIVA,
                @idPromotion = promotion?.IdPromotion
            });

            return result;
        }

        public async Task<int> CreateMovementCreditAsync(int idBillingCredit, int partialBalance, UserBillingInformation user, UserTypePlanInformation newUserTypePlan)
        {
            BillingCredit billingCredit = await GetBillingCredit(idBillingCredit);
            string conceptEnglish;
            string conceptSpanish;

            if (newUserTypePlan.IdUserType == UserTypeEnum.INDIVIDUAL)
            {
                conceptEnglish = "Credits Accreditation";
                conceptSpanish = "Acreditación de Créditos";
            }
            else
            {
                TextInfo textInfo = new CultureInfo("es", false).TextInfo;
                var date = billingCredit.ActivationDate ?? DateTime.UtcNow;
                conceptSpanish = "Acreditación de Emails Mes: " + textInfo.ToTitleCase(date.ToString("MMMM", CultureInfo.CreateSpecificCulture("es")));
                conceptEnglish = "Monthly Emails Accreditation: " + date.ToString("MMMM", CultureInfo.CreateSpecificCulture("en"));
            }

            var connection = _connectionFactory.GetConnection();
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
                @idUser = billingCredit.IdUser,
                @date = billingCredit.ActivationDate.HasValue ? billingCredit.ActivationDate.Value : DateTime.UtcNow,
                @idUserType = newUserTypePlan.IdUserType,
                @creditsQty = billingCredit.TotalCreditsQty.Value,
                @idBillingCredit = billingCredit.IdBillingCredit,
                @partialBalance = partialBalance + billingCredit.TotalCreditsQty.Value,
                @conceptEnglish = conceptEnglish,
                @conceptSpanish = conceptSpanish,
            });

            return result.FirstOrDefault();
        }

        public async Task<BillingCredit> GetBillingCredit(int billingCreditId)
        {
            using var connection = _connectionFactory.GetConnection();
            var billingCredit = await connection.QueryFirstOrDefaultAsync<BillingCredit>(@"
SELECT
    BC.[IdBillingCredit],
    BC.[Date],
    BC.[IdUser],
    BC.[PlanFee],
    BC.[CreditsQty],
    BC.[ActivationDate],
    BC.[TotalCreditsQty],
    BC.[IdUserTypePlan],
    DP.[DiscountPlanFee],
    BC.[IdResponsabileBilling],
    BC.[CCIdentificationType],
    BC.TotalMonthPlan
FROM
    [dbo].[BillingCredits] BC
        LEFT JOIN [dbo].[DiscountXPlan] DP
        ON BC.IdDiscountPlan = DP.IdDiscountPlan
WHERE
    IdBillingCredit = @billingCreditId",
                new
                {
                    @billingCreditId = billingCreditId
                });

            return billingCredit;
        }

        public async Task<PlanDiscountInformation> GetPlanDiscountInformation(int discountId)
        {
            using var connection = _connectionFactory.GetConnection();
            var discountInformation = await connection.QueryFirstOrDefaultAsync<PlanDiscountInformation>(@"
SELECT
    DP.[IdDiscountPlan],
    DP.[DiscountPlanFee],
    DP.[MonthPlan],
    DP.[ApplyPromo]
FROM
    [DiscountXPlan] DP
WHERE
    DP.[IdDiscountPlan] = @discountId AND DP.[Active] = 1",
    new { discountId });

            return discountInformation;
        }

        public async Task UpdateUserSubscriberLimitsAsync(int idUser)
        {
            using var connection = _connectionFactory.GetConnection();
            using var dtUserCheckLimits = new DataTable();
            dtUserCheckLimits.Columns.Add(new DataColumn("IdUser", typeof(int)));

            var dataRow = dtUserCheckLimits.NewRow();
            dataRow["IdUser"] = idUser;
            dtUserCheckLimits.Rows.Add(dataRow);

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Table", dtUserCheckLimits.AsTableValuedParameter("TYPEUSERTOCHECKLIMITS"));

            await connection.ExecuteAsync("User_UpdateLimits", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> ActivateStandBySubscribers(int idUser)
        {
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.ExecuteScalarAsync<int>("UserReactivateStandBySubscribers", new { IdUser = idUser }, commandType: CommandType.StoredProcedure);
            return result;
        }
    }
}
