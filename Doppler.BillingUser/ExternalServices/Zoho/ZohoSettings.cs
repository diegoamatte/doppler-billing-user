namespace Doppler.BillingUser.ExternalServices.Zoho
{
    public class ZohoSettings
    {
        public bool UseZoho { get; set; }
        public string BaseUrl { get; set; }
        public string AuthenticationUrl { get; set; }
        public string ZohoClientId { get; set; }
        public string ZohoClientSecret { get; set; }
        public string ZohoRefreshToken { get; set; }
    }
}
