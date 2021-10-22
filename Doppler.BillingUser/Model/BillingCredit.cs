namespace Doppler.BillingUser.Model
{
    public class BillingCredit
    {
        public int IdBillingCredit { get; set; }
        public System.DateTime Date { get; set; }
        public int IdUser { get; set; }
        public int IdUserType { get; set; }
        public int? IdUserTypePlan { get; set; }
        public int IdPaymentMethod { get; set; }
        public double? PlanFee { get; set; }
        public System.DateTime? PaymentDate { get; set; }
        public double? Taxes { get; set; }
        public int? IdCurrencyType { get; set; }
        public int? CreditsQty { get; set; }
        public System.DateTime? ActivationDate { get; set; }
        public int? IdPromotion { get; set; }
        public double? ExtraEmailFee { get; set; }
        public int? TotalCreditsQty { get; set; }
        public string CCNumber { get; set; }
        public short? CCExpMonth { get; set; }
        public short? CCExpYear { get; set; }
        public string CCVerification { get; set; }
        public int? IdCCType { get; set; }
        public int? IdConsumerType { get; set; }
        public string RazonSocial { get; set; }
        public string CUIT { get; set; }
        public int? IdBillingCreditType { get; set; }
        public string ExclusiveMessage { get; set; }
        public int? DiscountPlanFeePromotion { get; set; }
        public int? DiscountPlanFeeAdmin { get; set; }
        public int? ExtraCreditsPromotion { get; set; }
        public int? SubscribersQty { get; set; }
        public string CCHolderFullName { get; set; }
        public string NroFacturacion { get; set; }
        public int? IdDiscountPlan { get; set; }
        public int? TotalMonthPlan { get; set; }
        public int? CurrentMonthPlan { get; set; }
        public int? PromotionDuration { get; set; }
        public string PaymentType { get; set; }
        public string PaymentWay { get; set; }
        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public string CFDIUse { get; set; }
        public int? IdResponsabileBilling { get; set; }
        public string CCIdentificationType { get; set; }
        public string CCIdentificationNumber { get; set; }
        public bool? ResponsableIVA { get; set; }
    }
}
