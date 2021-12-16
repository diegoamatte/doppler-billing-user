using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Infrastructure
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public PromotionRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task UpdateTimeToUse(Promotion promocode, string operation)
        {
            using var connection = await _connectionFactory.GetConnection();
            await connection.ExecuteAsync(@$"
UPDATE
    [Promotions]
SET
    [TimesUsed] = [TimesUsed] {operation} 1
WHERE [IdPromotion] = @promocodeId", new
            {
                @promocodeId = promocode.IdPromotion
            });
        }
    }
}
