using System.Collections.Generic;
using Newtonsoft.Json;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoUpdateResponse
    {
        [JsonProperty(PropertyName = "data")]
        public List<ZohoUpdateResponseItem> Data { get; set; }
    }
}
