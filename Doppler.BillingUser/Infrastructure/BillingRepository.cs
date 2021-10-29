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
    U.CUIT as IdentificationNumber
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
    CUIT = @cuit
WHERE
    IdUser = @IdUser;",
                new
                {
                    user.IdUser,
                    @paymentMethodName = paymentMethod.PaymentMethodName,
                    @razonSocial = paymentMethod.RazonSocial,
                    @idConsumerType = paymentMethod.IdConsumerType,
                    @idResponsabileBilling = (int)ResponsabileBillingEnum.GBBISIDE,
                    @cuit = paymentMethod.IdentificationNumber
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
            sapDto.PaymentMethod = userData.PaymentMethod;
            sapDto.PlanType = userData.IdUserType;
            sapDto.BillingSystemId = userData.IdResponsabileBilling;
            sapDto.BillingStateId = ((sapDto.BillingSystemId == (int)ResponsabileBillingEnum.QBL || sapDto.BillingSystemId == (int)ResponsabileBillingEnum.QuickBookUSA) && sapDto.BillingCountryCode != "US") ? string.Empty
                : (sapDto.BillingCountryCode == "US") ? (SapDictionary.StatesDictionary.TryGetValue(userData.IdBillingState, out string stateIdUs) ? stateIdUs : string.Empty)
                : (SapDictionary.StatesDictionary.TryGetValue(userData.IdBillingState, out string stateId) ? stateId : "99");
            sapDto.County = userData.BillingStateName ?? "";
            sapDto.BillingCity = userData.BillingCity ?? "";

            await _sapService.SendUserDataToSap(sapDto);
        }
    }
}
