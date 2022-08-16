using Dapper;
using Doppler.BillingUser.Model;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public CurrencyRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<decimal> GetCurrencyRateAsync(int idCurrencyTypeFrom, int idCurrencyTypeTo, DateTime date)
        {
            using var connection = _connectionFactory.GetConnection();
            var rate = await connection.QueryFirstOrDefaultAsync<CurrencyRate>(@"
SELECT
    [IdCurrencyRate],
    [IdCurrencyTypeFrom],
    [IdCurrencyTypeTo],
    [Rate],
    [UTCFromDate],
    [Active]
FROM
    [CurrencyRate] R
WHERE
    ((R.IdCurrencyTypeFrom = @idCurrencyTypeFrom AND R.IdCurrencyTypeTo = idCurrencyTypeTo) OR
    (R.IdCurrencyTypeFrom = idCurrencyTypeTo AND R.IdCurrencyTypeTo = @idCurrencyTypeFrom)) AND
    R.UTCFromDate <= @date
ORDER BY R.UTCFromDate DESC",
                new
                {
                    idCurrencyTypeFrom,
                    idCurrencyTypeTo,
                    date
                });

            return rate == null
                ? 1
                : rate.IdCurrencyTypeFrom == idCurrencyTypeFrom && rate.IdCurrencyTypeTo == idCurrencyTypeTo ? rate.Rate : 1 / rate.Rate;
        }

        public async Task<decimal> ConvertCurrencyAsync(int idCurrencyTypeFrom, int idCurrencyTypeTo, decimal amount, DateTime date, decimal? rate)
        {
            if (!rate.HasValue)
            {
                rate = await GetCurrencyRateAsync(idCurrencyTypeFrom, idCurrencyTypeTo, date);
            }

            return decimal.Round(amount * rate.Value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
