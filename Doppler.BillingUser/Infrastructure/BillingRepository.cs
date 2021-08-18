using Dapper;
using Doppler.BillingUser.Model;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Utils;

namespace Doppler.BillingUser.Infrastructure
{
    public class BillingRepository : IBillingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IEncryptionService _encryptionService;

        public BillingRepository(IDatabaseConnectionFactory connectionFactory, IEncryptionService encryptionService)
        {
            _connectionFactory = connectionFactory;
            _encryptionService = encryptionService;
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
    B.CCHolderFullName,
    B.CCNumber,
    B.CCExpMonth,
    B.CCExpYear,
    B.CCVerification,
    C.[Description] AS CCType,
    P.PaymentMethodName AS PaymentMethodName,
    D.MonthPlan AS RenewalMonth,
    B.RazonSocial,
    B.IdConsumerType,
    B.CCIdentificationType AS IdentificationType,
    ISNULL(B.CUIT, B.CCIdentificationNumber) AS IdentificationNumber
FROM
    [BillingCredits] B
LEFT JOIN
    [PaymentMethods] P ON P.IdPaymentMethod = B.IdPaymentMethod
LEFT JOIN
    [CreditCardTypes] C ON C.IdCCType = B.IdCCType
LEFT JOIN
    [DiscountXPlan] D ON D.IdDiscountPlan = B.IdDiscountPlan
WHERE
    B.IdUser = (SELECT IdUser FROM [User] WHERE Email = @email) ORDER BY [Date] DESC;",
                new
                {
                    @email = username
                });

            if (result is not { PaymentMethodName: "CC" or "MP" })
                return result;

            result.CCHolderFullName = _encryptionService.DecryptAES256(result.CCHolderFullName);
            result.CCNumber = CreditCardHelper.ObfuscateNumber(_encryptionService.DecryptAES256(result.CCNumber));
            result.CCVerification = CreditCardHelper.ObfuscateVerificationCode(_encryptionService.DecryptAES256(result.CCVerification));

            return result;
        }
    }
}
