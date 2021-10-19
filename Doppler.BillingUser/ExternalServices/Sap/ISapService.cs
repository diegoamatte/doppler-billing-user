using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public interface ISapService
    {
        Task SendUserDataToSap(SapBusinessPartner sapBusinessPartner, string resultMessage = null);
    }
}
