namespace Doppler.BillingUser.Authorization
{
    public interface IJwtTokenGenerator
    {
        string GenerateSuperUserJwtToken();
    }
}
