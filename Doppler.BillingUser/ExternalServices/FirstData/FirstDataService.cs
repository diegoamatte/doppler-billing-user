using Doppler.BillingUser.Encryption;
using Microsoft.Extensions.Options;
using System.Globalization;

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

        public string GetUsername() => _encryptionService.DecryptAES256(_options.Value.FirstDataUsername);

        public string GetPassword() => _encryptionService.DecryptAES256(_options.Value.FirstDataPassword);

        public string GetHmac() => _encryptionService.DecryptAES256(_options.Value.FirstDataHmac);

        public string GetKeyId() => _encryptionService.DecryptAES256(_options.Value.FirstDataKeyId);

        public bool GetIsDemo() => _options.Value.FirstDataDemo;

        public int GetAmountToValidate() => int.Parse(_options.Value.FirstDataAmountToValidate, CultureInfo.InvariantCulture);

        public string GetFirstDataServiceSoap() => _options.Value.FirstDataServiceSoap;

        public string GetFirstDataServiceSoapDemo() => _options.Value.FirstDataServiceSoapDemo;
    }
}
