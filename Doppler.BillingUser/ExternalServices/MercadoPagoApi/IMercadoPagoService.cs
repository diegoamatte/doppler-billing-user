using System.Threading.Tasks;
using Doppler.BillingUser.ExternalServices.FirstData;

namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi
{
    public interface IMercadoPagoService
    {
        Task<MercadoPagoPayment> GetPaymentById(long id, string accountname);
        Task<MercadoPagoPayment> CreatePayment(string accountname, int clientId, decimal total, CreditCard creditCard);
    }
}
