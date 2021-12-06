using System.Threading.Tasks;

namespace Doppler.BillingUser.Authorization
{
    public interface IUsersApiTokenGetter
    {
        public Task<string> GetTokenAsync();
    }
}
