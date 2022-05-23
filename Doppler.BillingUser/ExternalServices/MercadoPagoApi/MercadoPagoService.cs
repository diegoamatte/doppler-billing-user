using Microsoft.Extensions.Options;

namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly IOptions<MercadoPagoSettings> _options;

        public MercadoPagoService(IOptions<MercadoPagoSettings> options)
        {
            _options = options;
        }
    }
}
