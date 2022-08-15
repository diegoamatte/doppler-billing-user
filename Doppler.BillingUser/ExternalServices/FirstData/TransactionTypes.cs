namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public class TransactionTypes
    {
        public const string Purchase = "00";

        public const string PreAuth = "01";

        public const string PreAuthComp = "02";

        public const string Forced = "03";

        public const string Refund = "04";

        public const string Auth = "05";

        public const string Void = "13";

        public const string TaggedPreAuth = "32";

        public const string TaggedVoid = "33";

        public const string TaggedRefund = "34";

        public const string CashOut = "83";

        public const string ValueLinkActivation = "85";

        public const string BalanceInquiry = "86";

        public const string Reload = "88";

        public const string ValueLinkDeactivation = "89";
    }
}
