namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public class TransactionTypes
    {
        public const string PURCHASE = "00";

        public const string PRE_AUTH = "01";

        public const string PRE_AUTH_COMP = "02";

        public const string FORCED = "03";

        public const string REFUND = "04";

        public const string AUTH = "05";

        public const string VOID = "13";

        public const string TAGGED_PRE_AUTH = "32";

        public const string TAGGED_VOID = "33";

        public const string TAGGED_REFUND = "34";

        public const string CASH_OUT = "83";

        public const string VALUE_LINK_ACTIVATION = "85";

        public const string BALANCE_INQUIRY = "86";

        public const string RELOAD = "88";

        public const string VALUE_LINK_DEACTIVATION = "89";
    }
}
