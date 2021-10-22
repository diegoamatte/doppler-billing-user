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
    public class UserRepository : IUserRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        #region AGREEMENT

        public async Task<User> GetUserForBillingCredit(string accountName, AgreementInformation agreementInformation)
        {
            using var connection = await _connectionFactory.GetConnection();
            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT
    U.IdUser,
    U.ResponsableIVA,
    U.PaymentType,
    U.PaymentWay,
    U.BankName,
    U.BankAccount,
    U.CFDIUse,
    U.UTCFirstPayment,
    U.Email,
    U.PaymentMethod,
    S.IdCountry
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

            user.NewUserTypePlan = await GetNewUserTypePlan(agreementInformation.PlanId);
            user.CurrentUserTypePlan = await GetCurrentUserTypePlan(user.IdUser);

            return user;
        }

        public async Task<UserTypePlan> GetNewUserTypePlan(int idUserTypePlan)
        {
            using var connection = await _connectionFactory.GetConnection();
            var userTypePlan = await connection.QueryFirstOrDefaultAsync<UserTypePlan>(@"
SELECT
    [IdUserTypePlan],
    [IdUserType],
    [Description],
    [EmailQty],
    [Fee],
    [ExtraEmailCost],
    [SubscribersQty]
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

        public async Task<UserTypePlan> GetCurrentUserTypePlan(int idUser)
        {
            using var connection = await _connectionFactory.GetConnection();
            var userTypePlan = await connection.QueryFirstOrDefaultAsync<UserTypePlan>(@"
SELECT TOP 1
    UTP.[IdUserTypePlan],
    UTP.[IdUserType],
    UTP.[Description],
    UTP.[EmailQty],
    UTP.[Fee],
    UTP.[ExtraEmailCost],
    UTP.[SubscribersQty]
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
                    @idUser = idUser
                });

            return userTypePlan;
        }

        public async Task<int> UpdateUserBillingCredit(User user)
        {
            var connection = await _connectionFactory.GetConnection();

            var result = await connection.ExecuteAsync(@"
UPDATE
    [dbo].[User]
SET
    CUIT = @cuit,
    IdCurrentBillingCredit = @idCurrentBillingCredit
WHERE
    IdUser = @idUser;",
            new
            {
                @idUser = user.IdUser,
                @IdCurrentBillingCredit = user.IdCurrentBillingCredit,
                @cuit = user.CUIT
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

        #endregion AGREEMENT
    }
}
