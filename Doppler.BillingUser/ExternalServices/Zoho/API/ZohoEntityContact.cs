using Newtonsoft.Json;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoEntityContact : ZohoEntityBase
    {
        [JsonProperty(PropertyName = "First_Name")]
        public string FirstName { get; set; }

        [JsonProperty(PropertyName = "Account_Name")]
        public ZohoUser AccountName { get; set; }

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
    }
}
