using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Doppler.BillingUser.Authorization
{
    public class CurrentRequestApiTokenGetter : ICurrentRequestApiTokenGetter
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentRequestApiTokenGetter(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

        public async Task<string> GetTokenAsync() => await _httpContextAccessor.HttpContext?.GetTokenAsync("Bearer", "access_token");
    }
}
