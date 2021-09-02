namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public class ResponseTypes
    {
        public const string NORMAL_TRANSACTION = "00";

        public const string INVALID_CC_NUMBER = "22";

        public const string INVALID_EXPIRY_DATE = "25";

        public const string INVALID_AMOUNT = "26";

        public const string INVALID_CARD_HOLDER = "27";

        public const string DUPLICATE = "63";
    }
}
