using System.Threading.Tasks;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IPromotionRepository
    {
        Task UpdateTimeToUse(Promotion promocode, string operation);
    }
}
