using System.Threading.Tasks;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IPromotionRepository
    {
        Task IncrementUsedTimes(Promotion promocode);
    }
}
