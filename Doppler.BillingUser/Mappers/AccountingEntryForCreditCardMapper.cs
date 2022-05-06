using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;

namespace Doppler.BillingUser.Mappers
{
    public class AccountingEntryForCreditCardMapper : IAccountingEntryMapper
    {
        private const string AccountingEntryTypeDescriptionInvoice = "Invoice";
        private const int UserAccountType = 1;
        private const int InvoiceBillingTypeQBL = 1;
        private const string AccountEntryTypeInvoice = "I";
        private const string AccountingEntryTypeDescriptionCCPayment = "CC Payment";
        private const string AccountEntryTypePayment = "P";
        private const string PaymentEntryTypePayment = "P";

        public AccountingEntry MapToInvoiceAccountingEntry(decimal total, UserBillingInformation user, CreditCard encryptedCreditCard, UserTypePlanInformation newPlan, CreditCardPayment payment)
        {
            return new AccountingEntry
            {
                IdClient = user.IdUser,
                Amount = total,
                Date = DateTime.UtcNow,
                Status = payment.Status.ToString(),
                Source = SourceTypeHelper.SourceTypeEnumMapper(newPlan),
                AccountingTypeDescription = AccountingEntryTypeDescriptionInvoice,
                InvoiceNumber = 0,
                IdAccountType = UserAccountType,
                IdInvoiceBillingType = InvoiceBillingTypeQBL,
                AuthorizationNumber = payment.AuthorizationNumber,
                AccountEntryType = AccountEntryTypeInvoice
            };
        }

        public AccountingEntry MapToPaymentAccountingEntry(decimal total, UserBillingInformation user, CreditCard encryptedCreditCard, UserTypePlanInformation newPlan, CreditCardPayment payment)
        {
            return new AccountingEntry
            {
                IdClient = user.IdUser,
                Amount = total,
                CcCNumber = encryptedCreditCard.Number,
                CcExpMonth = encryptedCreditCard.ExpirationMonth,
                CcExpYear = encryptedCreditCard.ExpirationYear,
                CcHolderName = encryptedCreditCard.HolderName,
                Date = DateTime.UtcNow,
                Source = SourceTypeHelper.SourceTypeEnumMapper(newPlan),
                AccountingTypeDescription = AccountingEntryTypeDescriptionCCPayment,
                IdAccountType = UserAccountType,
                IdInvoiceBillingType = InvoiceBillingTypeQBL,
                AccountEntryType = AccountEntryTypePayment,
                AuthorizationNumber = payment.AuthorizationNumber,
                PaymentEntryType = PaymentEntryTypePayment
            };
        }
    }
}
