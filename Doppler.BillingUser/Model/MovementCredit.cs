using System;

namespace Doppler.BillingUser.Model
{
    public class MovementCredit
    {
        public int IdMovementCredit { get; set; }
        public int IdUser { get; set; }
        public DateTime Date { get; set; }
        public int CreditsQty { get; set; }
        public int? IdCampaign { get; set; }
        public int? IdBillingCredit { get; set; }
        public int PartialBalance { get; set; }
        public int? IdAdmin { get; set; }
        public int? IdUserType { get; set; }
        public bool? Visible { get; set; }
        public string ConceptEnglish { get; set; }
        public string ConceptSpanish { get; set; }
    }
}
