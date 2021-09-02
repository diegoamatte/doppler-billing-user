namespace Doppler.BillingUser.ExternalServices.Sap
{
    public interface ISapService
    {
        void SendUserDataToSap(SapBusinessPartner sapBusinessPartner, string resultMessage = null);
    }
}
