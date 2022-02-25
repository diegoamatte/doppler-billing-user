using System.Data;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IDatabaseConnectionFactory
    {
        IDbConnection GetConnection();
    }
}
