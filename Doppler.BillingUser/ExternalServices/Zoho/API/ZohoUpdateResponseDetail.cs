using Newtonsoft.Json;
using System;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoUpdateResponseDetail
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Modified_Time")]
        public DateTime ModifiedTime { get; set; }

        [JsonProperty(PropertyName = "Modified_By")]
        public ZohoUser ModifiedBy { get; set; }

        [JsonProperty(PropertyName = "Created_Time")]
        public DateTime CreatedTime { get; set; }

        [JsonProperty(PropertyName = "Created_By")]
        public ZohoUser CreatedBy { get; set; }
    }
}
