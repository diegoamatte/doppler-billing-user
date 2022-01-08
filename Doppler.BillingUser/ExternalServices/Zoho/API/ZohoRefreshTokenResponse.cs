using Newtonsoft.Json;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoRefreshTokenResponse
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "api_domain")]
        public string ApiDomain { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }
    }
}
