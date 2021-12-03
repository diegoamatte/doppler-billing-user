using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserRepository
    {
        Task<UserBillingInformation> GetUserBillingInformation(string accountName);
        Task<UserTypePlanInformation> GetUserCurrentTypePlan(int idUser);
        Task<CreditCard> GetEncryptedCreditCard(string accountName);
        Task<UserTypePlanInformation> GetUserNewTypePlan(int idUserTypePlan);
    }
}
