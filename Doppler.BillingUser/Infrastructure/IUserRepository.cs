using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserRepository
    {
        Task<User> GetUserForBillingCredit(string accountName, AgreementInformation agreementInformation);
        Task<UserTypePlan> GetNewUserTypePlan(int idUserTypePlan);
        Task<UserTypePlan> GetCurrentUserTypePlan(int idUser);
        Task<int> UpdateUserBillingCredit(User user);
        Task<int> GetAvailableCredit(int idUser);
        Task<CreditCard> GetEncryptedCreditCard(string accountName);
    }
}
