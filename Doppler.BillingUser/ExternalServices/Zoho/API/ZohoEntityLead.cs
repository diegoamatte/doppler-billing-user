using Newtonsoft.Json;
using System;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoEntityLead : ZohoEntityBase
    {
        [JsonProperty(PropertyName = "First_Name")]
        public string FirstName { get; set; }

        [JsonProperty(PropertyName = "Last_Name")]
        public string LastName { get; set; }

        [JsonProperty(PropertyName = "Email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "Title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "Lead_Source")]
        public string Source { get; set; }

        [JsonProperty(PropertyName = "Company")]
        public string Company { get; set; }

        [JsonProperty(PropertyName = "Phone")]
        public string PhoneNumber { get; set; }

        [JsonProperty(PropertyName = "Mobile")]
        public string MobileNumber { get; set; }

        [JsonProperty(PropertyName = "Street")]
        public string Street { get; set; }

        [JsonProperty(PropertyName = "City")]
        public string City { get; set; }

        [JsonProperty(PropertyName = "Country")]
        public string Country { get; set; }

        [JsonProperty(PropertyName = "State")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "Zip")]
        public string Zip { get; set; }

        [JsonProperty(PropertyName = "Salutation")]
        public string Salutation { get; set; }

        [JsonProperty(PropertyName = "Website")]
        public string Website { get; set; }

        [JsonProperty(PropertyName = "Doppler")]
        public string Doppler { get; set; }

        [JsonProperty(PropertyName = "D_Created_Date")]
        public DateTime? DCreatedDate { get; set; }

        [JsonProperty(PropertyName = "D_Status")]
        public string DStatus { get; set; }

        [JsonProperty(PropertyName = "D_First_Payment")]
        public DateTime? DFirstPayment { get; set; }

        [JsonProperty(PropertyName = "D_DiscountType")]
        public string DDiscountType { get; set; }

        [JsonProperty(PropertyName = "D_DiscountTypeDesc")]
        public string DDiscountTypeDesc { get; set; }

        [JsonProperty(PropertyName = "D_Billing_System")]
        public string DBillingSystem { get; set; }

        [JsonProperty(PropertyName = "D_Origin")]
        public string DOrigin { get; set; }

        [JsonProperty(PropertyName = "D_Origin_Cookies")]
        public string DOriginCookies { get; set; }

        [JsonProperty(PropertyName = "D_Upgrade_Date")]
        public DateTime? DUpgradeDate { get; set; }

        [JsonProperty(PropertyName = "D_UserId")]
        public int? DUserId { get; set; }

        [JsonProperty(PropertyName = "D_PromoCode")]
        public string DPromoCode { get; set; }

        [JsonProperty(PropertyName = "D_Confirmation")]
        public DateTime? DConfirmation { get; set; }

        [JsonProperty(PropertyName = "D_Last_Login_2")]
        public DateTime? DLastLogin { get; set; }

        [JsonProperty(PropertyName = "D_Cant_Login")]
        public int? DCantLogin { get; set; }

        [JsonProperty(PropertyName = "D_Campa_as")]
        public int? DCampaigns { get; set; }

        [JsonProperty(PropertyName = "D_Creacion_lista")]
        public string DListCreated { get; set; }

        [JsonProperty(PropertyName = "Industria")]
        public string Industry { get; set; }

        [JsonProperty(PropertyName = "D_Integraciones")]
        public string DIntegraciones { get; set; }

        [JsonProperty(PropertyName = "D_DKIM_SPF")]
        public string DDkimSpf { get; set; }

        [JsonProperty(PropertyName = "Capsulas")]
        public int? Capsulas { get; set; }

        [JsonProperty(PropertyName = "D_Domain_Score2")]
        public int? DDomainScore { get; set; }

        [JsonProperty(PropertyName = "D_Last_Price_Visit")]
        public DateTime? DLastPriceVisit { get; set; }

        [JsonProperty(PropertyName = "D_Cant_Visits_Prices")]
        public int? DCantVisitsPrices { get; set; }

        [JsonProperty(PropertyName = "UTM_Source")]
        public string UTMSource { get; set; }

        [JsonProperty(PropertyName = "UTM_Medium")]
        public string UTMMedium { get; set; }

        [JsonProperty(PropertyName = "UTM_Campaign")]
        public string UTMCampaign { get; set; }

        [JsonProperty(PropertyName = "UTM_Term")]
        public string UTMTerm { get; set; }

        [JsonProperty(PropertyName = "UTM_Cookie1")]
        public string UTMCookies { get; set; }

        [JsonProperty(PropertyName = "UTM_Content")]
        public string UTMContent { get; internal set; }

        [JsonProperty(PropertyName = "D_Primera_Base")]
        public int DCantSubscribers { get; set; }

        [JsonProperty(PropertyName = "Lim500free")]
        public string Limit500Free { get; set; }

        [JsonProperty(PropertyName = "Origin_Inbound")]
        public string OriginInbound { get; set; }
    }
}
