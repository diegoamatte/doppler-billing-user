using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Mappers
{
    public interface IAccountingEntryMapper
    {
        AccountingEntry MapToInvoiceAccountingEntry(decimal total, UserBillingInformation user, CreditCard encryptedCreditCard, UserTypePlanInformation newPlan, CreditCardPayment payment);
        AccountingEntry MapToPaymentAccountingEntry(decimal total, UserBillingInformation user, CreditCard encryptedCreditCard, UserTypePlanInformation newPlan, CreditCardPayment payment);

    }
}
