using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Model
{
    public class CreditCardPayment
    {
        public string AuthorizationNumber { get; set; }
        public PaymentStatusEnum Status { get; set; }
        public string StatusDetails { get; set; }
    }
}
