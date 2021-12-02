namespace Doppler.BillingUser.ExternalServices.EmailSender
{
    public class Attachment
    {
        public string ContentType { get; set; }
        public string Filename { get; set; }
        public byte[] Content { get; set; }
    }
}
