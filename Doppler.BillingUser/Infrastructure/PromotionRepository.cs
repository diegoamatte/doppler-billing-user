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

        public async Task IncrementUsedTimes(Promotion promocode)
        {
            using var connection = _connectionFactory.GetConnection();
            await connection.ExecuteAsync(@"
UPDATE
    [Promotions]
SET
    [TimesUsed] = [TimesUsed] + 1
WHERE [IdPromotion] = @promocodeId", new
            {
                @promocodeId = promocode.IdPromotion
            });
        }
    }
}
