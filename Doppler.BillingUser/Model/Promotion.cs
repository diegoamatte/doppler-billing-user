namespace Doppler.BillingUser.Model
{
    public class Promotion
    {
        public int IdPromotion { get; set; }
        public string Code { get; set; }
        public int? ExtraCredits { get; set; }
        public int? DiscountPercentage { get; set; }
        public int? Duration { get; set; }
    }
}
