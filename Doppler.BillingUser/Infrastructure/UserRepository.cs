using Dapper;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class UserRepository : IUserRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserBillingInformation> GetUserBillingInformation(string accountName)
        {
            using var connection = await _connectionFactory.GetConnection();
            var user = await connection.QueryFirstOrDefaultAsync<UserBillingInformation>(@"
SELECT
    U.IdUser,
    U.PaymentMethod,
    U.ResponsableIVA,
    U.PaymentType,
    U.PaymentWay,
    U.BankName,
    U.BankAccount,
    U.CFDIUse,
    U.Email,
    S.IdCountry,
    U.UTCFirstPayment
FROM
    [User] U
    INNER JOIN
        State S ON U.IdState = S.IdState
WHERE
    U.Email = @email;",
                new
                {
                    @email = accountName
                });

            return user;
        }

        public async Task<UserTypePlanInformation> GetUserCurrentTypePlan(int idUser)
        {
            using var connection = await _connectionFactory.GetConnection();
            var userTypePlan = await connection.QueryFirstOrDefaultAsync<UserTypePlanInformation>(@"
SELECT TOP 1
    UTP.[IdUserType]
FROM
    [dbo].[BillingCredits] BC
    INNER JOIN
    [dbo].[UserTypesPlans] UTP
    ON
    BC.IdUserTypePlan = UTP.IdUserTypePlan
WHERE
    BC.[IdUser] = @idUser
ORDER BY
    BC.[Date] DESC;",
                new
                {
                    idUser
                });

            return userTypePlan;
        }

        public async Task<CreditCard> GetEncryptedCreditCard(string accountName)
        {
            using var connection = await _connectionFactory.GetConnection();
            var encryptedCreditCard = await connection.QueryFirstOrDefaultAsync<CreditCard>(@"
SELECT
    CCHolderFullName as HolderName,
    CCNumber as Number,
    CCExpMonth as ExpirationMonth,
    CCExpYear as ExpirationYear,
    CCVerification as Code,
    IdCCType as CardType
FROM
    [User]
WHERE
    Email = @email;",
                new
                {
                    @email = accountName
                });

            return encryptedCreditCard;
        }

        public async Task<UserTypePlanInformation> GetUserNewTypePlan(int idUserTypePlan)
        {
            using var connection = await _connectionFactory.GetConnection();
            var userTypePlan = await connection.QueryFirstOrDefaultAsync<UserTypePlanInformation>(@"
SELECT
    [IdUserTypePlan],
    [IdUserType],
    [EmailQty],
    [Fee],
    [ExtraEmailCost],
    [SubscribersQty],
    [Description] as Subscribers
FROM
    [dbo].[UserTypesPlans]
WHERE
    [IdUserTypePlan] = @idUserTypePlan;",
                new
                {
                    @idUserTypePlan = idUserTypePlan
                });

            return userTypePlan;
        }

        public async Task<int> UpdateUserBillingCredit(UserBillingInformation user)
        {
            var connection = await _connectionFactory.GetConnection();
            var result = await connection.ExecuteAsync(@"
UPDATE
    [dbo].[User]
SET
    CUIT = @cuit,
    UTCFirstPayment = @utfFirstPayment,
    IdCurrentBillingCredit = @idCurrentBillingCredit
WHERE
    IdUser = @idUser;",
            new
            {
                @idUser = user.IdUser,
                @IdCurrentBillingCredit = user.IdCurrentBillingCredit,
                @cuit = user.Cuit,
                @utfFirstPayment = user.UTCFirstPayment ?? DateTime.UtcNow,
            });

            return result;
        }

        public async Task<int> GetAvailableCredit(int idUser)
        {
            using var connection = await _connectionFactory.GetConnection();
            var partialBalance = await connection.QueryFirstOrDefaultAsync<int>(@"
SELECT
    PartialBalance
FROM
    [dbo].[MovementsCredits]
WHERE
    IdUser = @idUser
ORDER BY
    IdMovementCredit
DESC",
                new
                {
                    @idUser = idUser
                });

            return partialBalance;
        }

        public async Task<User> GetUserInformation(string accountName)
        {
            using var connection = await _connectionFactory.GetConnection();
            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT
    U.FirstName,
    U.LastName,
    U.Address,
    U.PhoneNumber,
    U.Company,
    U.CityName,
    BS.Name as BillingStateName,
    U.ZipCode,
    L.Name as Language,
    C.Name as BillingCountryName,
    V.Fullname as Vendor,
    U.CUIT,
    U.RazonSocial,
    U.IdConsumerType,
    U.BillingEmails
FROM
    [User] U
LEFT JOIN
    [Vendor] V ON V.IdVendor = U.IdVendor
LEFT JOIN
    [State] BS ON BS.IdState = U.IdBillingState
LEFT JOIN
    [Country] C ON C.IdCountry = BS.IdCountry
INNER JOIN
    Language L ON U.IdLanguage = L.IdLanguage
WHERE
    U.Email = @accountName;",
                new
                {
                    accountName
                });

            return user;
        }
    }
}
