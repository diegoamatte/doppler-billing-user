namespace Doppler.BillingUser.Model
{
    public class PaymentAmountDetail
    {
        public decimal Total { get; set; }
        public decimal Taxes { get; set; }
        public decimal CurrencyRate { get; set; }
    }
}
