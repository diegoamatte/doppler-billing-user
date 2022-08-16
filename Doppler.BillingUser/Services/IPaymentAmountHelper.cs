using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Services
{
    public interface IPaymentAmountHelper
    {
        Task<PaymentAmountDetail> ConvertCurrencyAmount(CurrencyType fromCurrencyType, CurrencyType toCurrencyType, decimal fromValue);
    }
}
