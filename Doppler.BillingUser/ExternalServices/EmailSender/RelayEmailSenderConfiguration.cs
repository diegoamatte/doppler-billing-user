namespace Doppler.BillingUser.ExternalServices.EmailSender
{
    public class RelayEmailSenderConfiguration
    {
        public string SendUrlTemplate { get; set; }
        public string SendTemplateUrlTemplate { get; set; }
        public string ApiKey { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string Username { get; set; }
        public string FromName { get; set; }
        public string FromAddress { get; set; }
        public string ReplyToAddress { get; set; }
    }
}
