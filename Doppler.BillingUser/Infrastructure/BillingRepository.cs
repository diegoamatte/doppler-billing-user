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
        private const int SourceTypeBuyCreditsId = 3;

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
            using var connection = await _connectionFactory.GetConnection();

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
            await SendUserDataToSap(accountName, paymentMethod.IdSelectedPlan);

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

        private async Task SendUserDataToSap(string accountName, int planId)
        {
            using var connection = await _connectionFactory.GetConnection();

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

        public async Task<int> CreateAccountingEntriesAsync(AgreementInformation agreementInformation, CreditCard encryptedCreditCard, int userId, string authorizationNumber)
        {
            var connection = await _connectionFactory.GetConnection();
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
                @source = SourceTypeBuyCreditsId,
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
                @source = SourceTypeBuyCreditsId,
                @accountingTypeDescription = AccountingEntryTypeDescriptionCCPayment,
                @idAccountType = UserAccountType,
                @idInvoiceBillingType = InvoiceBillingTypeQBL,
                @accountEntryType = AccountEntryTypePayment,
                @authorizationNumber = authorizationNumber,
                @paymentEntryType = PaymentEntryTypePayment
            });

            return invoiceId;
        }
    }
}
