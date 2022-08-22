using System.Threading.Tasks;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Services
{
    public interface IPaymentAmountHelper
    {
        Task<PaymentAmountDetail> ConvertCurrencyAmount(CurrencyType fromCurrencyType, CurrencyType toCurrencyType, decimal fromValue);
    }
}
