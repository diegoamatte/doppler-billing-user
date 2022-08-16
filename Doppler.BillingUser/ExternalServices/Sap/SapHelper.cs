using System;
using System.Linq;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public static class SapHelper
    {
        public static string GetFirstName(User user)
        {
            return !string.IsNullOrEmpty(user.RazonSocial)
                ? user.RazonSocial
                : !string.IsNullOrEmpty(user.BillingFirstName) ? user.BillingFirstName : user.FirstName ?? string.Empty;
        }

        public static string GetBillingStateId(User user)
        {
            if (user.BillingStateCountryCode != "US")
            {
                return string.Empty;
            }

            SapDictionary.StatesDictionary.TryGetValue(user.IdBillingState, out var stateIdUs);

            return !string.IsNullOrEmpty(stateIdUs) ? stateIdUs : "99";
        }

        public static bool IsMakingSenseAccount(string email)
        {
            var excludes = new[] { "@makingsense", "@fromdoppler", "@getcs", "@dopplerrelay", "@doppleracademy.com" };

            var result = excludes.Any(email.Contains);

            return result;
        }

        public static int GetPeriodicityToSap(int? monthPlan) => monthPlan switch { 3 => 1, 6 => 2, 12 => 3, _ => 0 };

        public static DateTime ToHourOffset(this DateTime date, int hours) =>
            new DateTimeOffset(date, TimeSpan.Zero).ToOffset(TimeSpan.FromHours(hours)).DateTime;
    }
}
