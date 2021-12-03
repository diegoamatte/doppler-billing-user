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

        Task<bool> UpdateCurrentPaymentMethod(string accountName, PaymentMethod paymentMethod);

        Task<EmailRecipients> GetInvoiceRecipients(string accountName);

        Task UpdateInvoiceRecipients(string accountName, string[] emailRecipients, int planId);

        Task<int> CreateAccountingEntriesAsync(AgreementInformation agreementInformation, CreditCard encryptedCreditCard, int userId, string authorizationNumber);
    }
}
