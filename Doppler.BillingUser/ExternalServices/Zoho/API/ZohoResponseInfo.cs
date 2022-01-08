using Newtonsoft.Json;


namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoResponseInfo
    {
        [JsonProperty(PropertyName = "per_page")]
        public int PerPage { get; set; }

        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "page")]
        public int Page { get; set; }

        [JsonProperty(PropertyName = "more_records")]
        public bool MoreRecords { get; set; }
    }
}
