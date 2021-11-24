using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserRepository
    {
        Task<UserBillingInformation> GetUserBillingInformation(string accountName);
    }
}
