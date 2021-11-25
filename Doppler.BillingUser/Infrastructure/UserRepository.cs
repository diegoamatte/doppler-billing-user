using Dapper;
using Doppler.BillingUser.Model;
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
    U.PaymentMethod
FROM
    [User] U
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
    }
}
