using System;

namespace Doppler.BillingUser.ExternalServices.Zoho
{
    public class ZohoDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string PhoneNumber { get; set; }
        public string ZipCode { get; set; }
        public string State { get; set; }
        public string Street { get; set; }
        // Specific Doppler Fields
        public string Doppler { get; set; }
        public DateTime CreationDate { get; set; }
        public string Status { get; set; }
        public DateTime FirstPaymentDate { get; set; }
        public string DiscountType { get; set; }
        public string BillingSystem { get; set; }
        public string Origin { get; set; }
        public string OriginFirst { get; set; }
        public DateTime UpgradeDate { get; set; }
        public int UserId { get; set; }
        public string PromoCodo { get; set; }
        public string DiscountTypeDescription { get; set; }
        public string Source { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public DateTime? LastLogin { get; set; }
        public int CantLogin { get; set; }
        public int CantCampaigns { get; set; }
        public bool ListCreated { get; set; }
        public string Industry { get; set; }
        public bool UseDKIM { get; set; }
        public bool UseIntegration { get; set; }
        public int CantCapsulas { get; set; }
        public int DomainScore { get; set; }
        public DateTime? LastPriceVisitDate { get; set; }
        public int PriceVisitCount { get; set; }
        public string UTMSource { get; set; }
        public string UTMMedium { get; set; }
        public string UTMCampaign { get; set; }
        public string UTMTerm { get; set; }
        public string UTMCookies { get; set; }
        public string UTMContent { get; set; }
        public int SubscribersCountFirstImport { get; set; }
        public bool SubscribersLimitExceed { get; set; }
        public string OriginInbound { get; set; }
    }
}
