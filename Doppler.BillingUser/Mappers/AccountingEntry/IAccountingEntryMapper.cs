using System.Threading.Tasks;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Mappers
{
    public interface IAccountingEntryMapper
    {
        Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, UserBillingInformation user, UserTypePlanInformation newPlan, CreditCardPayment payment);
        Task<AccountingEntry> MapToPaymentAccountingEntry(AccountingEntry invoiceEntry, CreditCard encryptedCreditCard);

    }
}
