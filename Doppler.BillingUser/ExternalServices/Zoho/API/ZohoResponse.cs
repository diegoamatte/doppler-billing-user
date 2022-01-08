using Newtonsoft.Json;
using System.Collections.Generic;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoResponse<T>
    {
        [JsonProperty(PropertyName = "data")]
        public List<T> Data { get; set; }

        [JsonProperty(PropertyName = "info")]
        public ZohoResponseInfo Info { get; set; }
    }
}
