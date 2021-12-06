using System.Threading.Tasks;

namespace Doppler.BillingUser.Authorization
{
    public interface IAccountPlansApiTokenGetter
    {
        public Task<string> GetTokenAsync();
    }
}
