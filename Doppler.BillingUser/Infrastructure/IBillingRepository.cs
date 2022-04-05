using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IBillingRepository
    {
        Task<BillingInformation> GetBillingInformation(string accountName);

        Task UpdateBillingInformation(string accountName, BillingInformation billingInformation);

        Task<PaymentMethod> GetCurrentPaymentMethod(string username);

        Task<bool> UpdateCurrentPaymentMethod(User user, PaymentMethod paymentMethod);

        Task<EmailRecipients> GetInvoiceRecipients(string accountName);

        Task UpdateInvoiceRecipients(string accountName, string[] emailRecipients, int planId);

        Task<int> CreateAccountingEntriesAsync(AgreementInformation agreementInformation, CreditCard encryptedCreditCard, int userId, UserTypePlanInformation newPlan, string authorizationNumber);
        Task<CurrentPlan> GetCurrentPlan(string accountName);

        Task<int> CreateBillingCreditAsync(AgreementInformation agreementInformation, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, Promotion promotion);

        Task<int> CreateMovementCreditAsync(int idBillingCredit, int partialBalance, UserBillingInformation user, UserTypePlanInformation newUserTypePlan);

        Task<BillingCredit> GetBillingCredit(int billingCreditId);

        Task UpdateUserSubscriberLimitsAsync(int idUser);

        Task<int> ActivateStandBySubscribers(int idUser);

        Task<PlanDiscountInformation> GetPlanDiscountInformation(int discountId);
    }
}
