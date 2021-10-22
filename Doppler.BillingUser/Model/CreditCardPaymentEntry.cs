using System;

namespace Doppler.BillingUser.Model
{
    public class CreditCardPaymentEntry
    {
        public string CCNumber { get; set; }
        public short? CCExpMonth { get; set; }
        public short? CCExpYear { get; set; }
        public string CCHolderName { get; set; }
        public string CCVerification { get; set; }
        public int? IdInvoice { get; set; }
        public int IdAccountingEntry { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int? IdBillingSource { get; set; }
        public int? Source { get; set; }
        public string AccountingTypeDescription { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public int IdAccountType { get; set; }
        public int? IdClient { get; set; }
        public int? IdCurrencyType { get; set; }
        public decimal? Taxes { get; set; }
        public decimal? CurrencyRate { get; set; }
        public string AuthorizationNumber { get; set; }
        public int IdInvoiceBillingType { get; set; }
        public bool? IsException { get; set; }
        public string AccountEntryType { get; set; }
        public string PaymentEntryType { get; set; }
    }
}
