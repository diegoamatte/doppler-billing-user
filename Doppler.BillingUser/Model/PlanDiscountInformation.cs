namespace Doppler.BillingUser.Model
{
    public class PlanDiscountInformation
    {
        public int IdDiscountPlan { get; set; }
        public decimal DiscountPlanFee { get; set; }
        public int MonthPlan { get; set; }
        public bool ApplyPromo { get; set; }
    }
}
