using Doppler.BillingUser.ExternalServices.Zoho.API;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.Zoho
{
    public class ZohoService : IZohoService
    {
        private readonly IOptions<ZohoSettings> _options;
        private readonly IFlurlClient _flurlZohoClient;
        private readonly IFlurlClient _flurlZohoAuthenticationClient;
        private string _accessToken;

        public ZohoService(
            IOptions<ZohoSettings> options,
            IFlurlClientFactory flurlClientFac)
        {
            _options = options;
            _flurlZohoClient = flurlClientFac.Get(_options.Value.BaseUrl);
            _flurlZohoAuthenticationClient = flurlClientFac.Get(_options.Value.AuthenticationUrl);
        }

        public async Task RefreshTokenAsync()
        {
            var response = await _flurlZohoAuthenticationClient.Request(new UriTemplate($"{_options.Value.AuthenticationUrl}").Resolve())
                .SetQueryParam("refresh_token", _options.Value.ZohoRefreshToken)
                .SetQueryParam("grant_type", "refresh_token")
                .SetQueryParam("scope", "ZohoCRM.modules.ALL,ZohoCRM.users.ALL")
                .SetQueryParam("client_id", _options.Value.ZohoClientId)
                .SetQueryParam("client_secret", _options.Value.ZohoClientSecret)
                .WithHeader("Authorization", $"Zoho-oauthtoken {_options.Value.ZohoRefreshToken}")
                .PostAsync().ReceiveJson<ZohoRefreshTokenResponse>();

            if (response != null)
            {
                _accessToken = response.AccessToken;
            }
        }

        public async Task<T> SearchZohoEntityAsync<T>(string moduleName, string criteria)
        {
            var entity = await _flurlZohoClient.Request(new UriTemplate($"{_options.Value.BaseUrl}{moduleName}/search").Resolve())
                .SetQueryParam("criteria", criteria)
                .WithHeader("Authorization", $"Zoho-oauthtoken {_accessToken}")
                .GetJsonAsync<T>();

            return entity;
        }

        public async Task<ZohoUpdateResponse> UpdateZohoEntityAsync(string body, string entityId, string moduleName)
        {
            var entity = await _flurlZohoClient.Request(new UriTemplate($"{_options.Value.BaseUrl}{moduleName}/{entityId}").Resolve())
                .WithHeader("Authorization", $"Zoho-oauthtoken {_accessToken}")
                .PutStringAsync(body).ReceiveJson<ZohoUpdateResponse>();
            return entity;
        }
    }
}
