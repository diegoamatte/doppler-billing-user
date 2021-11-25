using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Model
{
    public class UserBillingInformation
    {
        public int IdUser { get; set; }
        public PaymentMethodEnum PaymentMethod { get; set; }
    }
}
