namespace Doppler.BillingUser.Model
{
    public class UserTypePlan
    {
        public int? IdUserTypePlan { get; set; }
        public int IdUserType { get; set; }
        public string Description { get; set; }
        public int? EmailQty { get; set; }
        public double? Fee { get; set; }
        public double? ExtraEmailCost { get; set; }
        public int? SubscribersQty { get; set; }
    }
}
