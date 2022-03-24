using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Services
{
    public interface IEmailTemplatesService
    {
        Task<bool> SendCheckAndTransferPurchaseNotification(string language, string fistName, string planName, double amount, string paymentMethod, int creditsQuantity, string sendTo);
        Task<bool> SendCreditsApprovedAdminNotification(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode);
        Task<bool> SendNotificationForSuscribersPlan(string accountname, User userInformation, UserTypePlanInformation newPlan);
        Task<bool> SendActivatedStandByEmail(string language, string fistName, int standByAmount, string sendTo);
        Task SendNotificationForUpgradePlan(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId, PlanDiscountInformation planDiscountInformation);
        Task SendNotificationForCreditsApproved(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode);
    }
}
