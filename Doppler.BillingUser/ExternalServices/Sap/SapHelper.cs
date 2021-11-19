using Doppler.BillingUser.Enums;
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

        public static string GetBillingStateId(User user)
        {
            if (user.BillingStateCountryCode != "US")
                return string.Empty;

            SapDictionary.StatesDictionary.TryGetValue(user.IdBillingState, out var stateIdUs);

            return !string.IsNullOrEmpty(stateIdUs) ? stateIdUs : "99";
        }
    }
}
