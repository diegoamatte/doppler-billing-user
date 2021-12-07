using System.Threading.Tasks;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.ExternalServices.AccountPlansApi
{
    public interface IAccountPlansService
    {
        public Task<bool> IsValidTotal(string accountname, AgreementInformation agreementInformation);
    }
}
