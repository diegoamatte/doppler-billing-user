namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public interface IFirstDataService
    {
        public string GetUsername();
        public string GetPassword();
        public string GetHmac();
        public string GetKeyId();
        public bool GetIsDemo();
        public int GetAmountToValidate();
        public string GetFirstDataServiceSoap();
        public string GetFirstDataServiceSoapDemo();
    }
}
