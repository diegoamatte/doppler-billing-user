using Newtonsoft.Json;
using System.Collections.Generic;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoUpdateResponse
    {
        [JsonProperty(PropertyName = "data")]
        public List<ZohoUpdateResponseItem> Data { get; set; }
    }
}
