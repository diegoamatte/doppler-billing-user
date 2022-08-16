namespace Doppler.BillingUser.Utils
{
    public static class CreditCardHelper
    {
        public static string ObfuscateNumber(string cardNumber)
        {
            const int clearChars = 4;
            var start = cardNumber.Length - clearChars;
            return $"{new string('*', start)}{cardNumber.Substring(start, clearChars)}";
        }

        public static string ObfuscateVerificationCode(string code) => $"{new string('*', code.Length)}";
    }
}
