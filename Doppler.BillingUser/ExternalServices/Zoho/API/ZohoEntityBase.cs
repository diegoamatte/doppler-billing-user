using Newtonsoft.Json;
using System;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{

    public class ZohoDopplerValues
    {
        public const string Free = "Free";
        public const string Active = "Active";
        public const string Inactive = "Inactive";
        public const string Discount = "% Discount";
        public const string Credits = "Extra Credits";

    }
    public class ZohoEntityBase
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "owner")]
        public ZohoUser Owner { get; set; }

        [JsonProperty(PropertyName = "Created_By")]
        public ZohoUser CreatedBy { get; set; }

        [JsonProperty(PropertyName = "Modified_By")]
        public ZohoUser ModifiedBy { get; set; }

        [JsonProperty(PropertyName = "Created_Time")]
        public DateTime? CreatedTime { get; set; }

        [JsonProperty(PropertyName = "Modified_Time")]
        public DateTime? ModifiedTime { get; set; }

        [JsonProperty(PropertyName = "Last_Activity_Time")]
        public DateTime? LastActivityTime { get; set; }

    }
}
