using Newtonsoft.Json;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoUser
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
    }
}
