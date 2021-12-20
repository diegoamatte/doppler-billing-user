using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.Slack
{
    public interface ISlackService
    {
        Task SendNotification(string message = null);
    }
}
