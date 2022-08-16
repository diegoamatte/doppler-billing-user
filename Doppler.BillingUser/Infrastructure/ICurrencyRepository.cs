using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface ICurrencyRepository
    {
        Task<decimal> GetCurrencyRateAsync(int idCurrencyTypeFrom, int idCurrencyTypeTo, DateTime currencyDate);
        Task<decimal> ConvertCurrencyAsync(int idCurrencyTypeFrom, int idCurrencyTypeTo, decimal amount, DateTime currencyDate, decimal? rate);
    }
}
