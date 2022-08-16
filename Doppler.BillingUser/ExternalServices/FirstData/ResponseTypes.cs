namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public class ResponseTypes
    {
        public const string NormalTransaction = "00";

        public const string InvalidCcNumber = "22";

        public const string InvalidExpirydate = "25";

        public const string InvalidAmount = "26";

        public const string InvalidCardHolder = "27";

        public const string Duplicate = "63";
    }
}
