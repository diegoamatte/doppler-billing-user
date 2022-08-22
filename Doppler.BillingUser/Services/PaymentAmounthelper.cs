using System;
using System.Threading.Tasks;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Services
{
    public class PaymentAmounthelper : IPaymentAmountHelper
    {
        private readonly ICurrencyRepository _currencyRepository;
        private readonly decimal _taxRate = 0.21m;

        public PaymentAmounthelper(ICurrencyRepository currencyRepository)
        {
            _currencyRepository = currencyRepository;
        }

        public async Task<PaymentAmountDetail> ConvertCurrencyAmount(CurrencyType fromCurrencyType, CurrencyType toCurrencyType, decimal fromValue)
        {
            var currencyRate = await _currencyRepository.GetCurrencyRateAsync((int)fromCurrencyType, (int)toCurrencyType, DateTime.UtcNow);
            var totalWithoutTaxes = decimal.Round(fromValue * currencyRate, 2, MidpointRounding.AwayFromZero);
            var taxesInFromCurrency = decimal.Round(fromValue * _taxRate, 2, MidpointRounding.AwayFromZero);
            var taxes = decimal.Round(totalWithoutTaxes * _taxRate, 2, MidpointRounding.AwayFromZero);

            return new PaymentAmountDetail
            {
                Taxes = taxesInFromCurrency,
                CurrencyRate = currencyRate,
                Total = totalWithoutTaxes + taxes,
            };
        }
    }
}
