using System;

namespace Doppler.BillingUser.Model
{
    public class BillingCredit
    {
        public int IdBillingCredit { get; set; }
        public int IdUser { get; set; }
        public DateTime? ActivationDate { get; set; }
        public int? TotalCreditsQty { get; set; }
        public int? CreditsQty { get; set; }
        public int IdUserTypePlan { get; set; }
        public DateTime Date { get; set; }
        public decimal PlanFee { get; set; }
        public int DiscountPlanFee { get; set; }
        public int IdResponsabileBilling { get; set; }
    }
}
