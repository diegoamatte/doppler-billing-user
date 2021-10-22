namespace Doppler.BillingUser.Authorization
{
    public class JwtOptions
    {
        public string RsaParametersFilePath { get; set; }
        public long TokenLifeTime { get; set; }
    }
}
