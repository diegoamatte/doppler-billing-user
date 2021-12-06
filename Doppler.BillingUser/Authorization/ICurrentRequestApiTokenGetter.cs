using System.Threading.Tasks;

namespace Doppler.BillingUser.Authorization
{
    public interface ICurrentRequestApiTokenGetter
    {
        public Task<string> GetTokenAsync();
    }
}
