using System;

namespace Doppler.BillingUser.Model
{
    public class BillingCredit
    {
        public int IdBillingCredit { get; set; }
        public int IdUser { get; set; }
        public DateTime? ActivationDate { get; set; }
        public int? TotalCreditsQty { get; set; }
    }
}
