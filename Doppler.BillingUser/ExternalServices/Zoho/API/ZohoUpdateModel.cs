using System.Collections.Generic;
using Newtonsoft.Json;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoUpdateModel<T>
    {
        [JsonProperty(PropertyName = "data")]
        public List<T> Data { get; set; }
    }
}
