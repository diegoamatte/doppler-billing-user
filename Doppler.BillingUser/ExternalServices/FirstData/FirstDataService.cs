using Doppler.BillingUser.Encryption;
using Microsoft.Extensions.Options;

namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public class FirstDataService : IFirstDataService
    {
        private IEncryptionService _encryptionService;
        private readonly IOptions<FirstDataSettings> _options;

        public FirstDataService(IEncryptionService encryptionService, IOptions<FirstDataSettings> options)
        {
            _encryptionService = encryptionService;
            _options = options;
        }

        public string GetUsername()
        {
            return _encryptionService.DecryptAES256(_options.Value.FirstDataUsername);
        }

        public string GetPassword()
        {
            return _encryptionService.DecryptAES256(_options.Value.FirstDataPassword);
        }

        public string GetHmac()
        {
            return _encryptionService.DecryptAES256(_options.Value.FirstDataHmac);
        }

        public string GetKeyId()
        {
            return _encryptionService.DecryptAES256(_options.Value.FirstDataKeyId);
        }

        public bool GetIsDemo()
        {
            return _options.Value.FirstDataDemo;
        }

        public int GetAmountToValidate()
        {
            return int.Parse(_options.Value.FirstDataAmountToValidate);
        }

        public string GetFirstDataServiceSoap()
        {
            return _options.Value.FirstDataServiceSoap;
        }

        public string GetFirstDataServiceSoapDemo()
        {
            return _options.Value.FirstDataServiceSoapDemo;
        }
    }
}
