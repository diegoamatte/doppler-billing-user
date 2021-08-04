using Dapper;
using Doppler.BillingUser.Model;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class BillingRepository : IBillingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        public BillingRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<BillingInformation> GetBillingInformation(string email)
        {
            using (IDbConnection connection = await _connectionFactory.GetConnection())
            {
                var results = await connection.QueryAsync<BillingInformation>(@"
SELECT
    U.BillingFirstName AS Firstname,
    U.BillingLastName AS Lastname,
    U.Address,
    U.CityName AS City,
    isnull(S.Name, '') AS Province,
    isnull(CO.Code, '') AS Country,
    U.ZipCode,
    U.PhoneNumber AS Phone,
    U.IdSecurityQuestion AS ChooseQuestion,
    U.IdSecurityQuestion AS AnswerQuestion
FROM
    [User] U
    LEFT JOIN [State] S ON U.IdState = S.IdState
    LEFT JOIN [Country] CO ON S.IdCountry = CO.IdCountry
    LEFT JOIN [SecurityQuestion] SQ ON SQ.IdSecurityQuestion = U.IdSecurityQuestion
WHERE
    U.Email = @email",
                    new { email });
                return results.FirstOrDefault();
            }
        }
    }
}
