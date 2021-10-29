using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public static class SapHelper
    {
        public static string GetFirstName(User user)
        {
            if (!string.IsNullOrEmpty(user.RazonSocial))
                return user.RazonSocial;

            if (!string.IsNullOrEmpty(user.BillingFirstName))
                return user.BillingFirstName;

            return user.FirstName ?? string.Empty;
        }
    }
}
