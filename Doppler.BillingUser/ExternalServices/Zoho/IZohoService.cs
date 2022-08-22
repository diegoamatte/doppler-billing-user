using System.Threading.Tasks;
using Doppler.BillingUser.ExternalServices.Zoho.API;

namespace Doppler.BillingUser.ExternalServices.Zoho
{
    public interface IZohoService
    {
        Task<T> SearchZohoEntityAsync<T>(string moduleName, string criteria);
        Task<ZohoUpdateResponse> UpdateZohoEntityAsync(string body, string entityId, string moduleName);

        Task RefreshTokenAsync();
    }
}
