using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Doppler.BillingUser.Infrastructure
{
    public class DatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly string _connectionString;

        public DatabaseConnectionFactory(IOptions<DopplerDatabaseSettings> dopplerDataBaseSettings)
        {
            _connectionString = dopplerDataBaseSettings.Value.GetSqlConnectionString();
        }

        public IDbConnection GetConnection() => new SqlConnection(_connectionString);
    }
}
