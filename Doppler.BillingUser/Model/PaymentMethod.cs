namespace Doppler.BillingUser.Model
{
    public class PaymentMethod
    {
        public string CCHolderFullName { get; set; }
        public string CCNumber { get; set; }
        public string CCVerification { get; set; }
        public string CCExpMonth { get; set; }
        public string CCExpYear { get; set; }
        public string CCType { get; set; }
        public string PaymentMethodName { get; set; }
        public string RenewalMonth { get; set; }
        public string RazonSocial { get; set; }
        public string IdConsumerType { get; set; }
        public string IdentificationType { get; set; }
        public string IdentificationNumber { get; set; }
    }
}
