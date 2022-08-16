using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace Doppler.BillingUser.DopplerSecurity
{
    public class DopplerSecurityOptions
    {
        public IEnumerable<SecurityKey> SigningKeys { get; set; } = System.Array.Empty<SecurityKey>();
    }
}
