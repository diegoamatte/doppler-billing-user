using Doppler.BillingUser.ExternalServices.Zoho.API;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.Zoho
{
    public interface IZohoService
    {
        Task<T> SearchZohoEntityAsync<T>(string moduleName, string criteria);
        Task<ZohoUpdateResponse> UpdateZohoEntityAsync(string body, string entityId, string moduleName);
    }
}
