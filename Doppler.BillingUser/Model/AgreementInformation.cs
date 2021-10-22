namespace Doppler.BillingUser.Model
{
    public class AgreementInformation
    {
        public int PlanId { get; set; }
        public int DiscountId { get; set; }
        public double? Total { get; set; }

        //Payment
        public int InvoiceNumber { get; set; }
        public string TransferReference { get; set; }
        public string AuthorizationNumber { get; set; }
    }
}
