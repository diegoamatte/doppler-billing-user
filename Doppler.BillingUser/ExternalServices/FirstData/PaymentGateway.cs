using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.E4;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public class PaymentGateway : IPaymentGateway
    {
        private readonly ServiceSoapClient _orderService;
        private readonly string _gatewayId;
        private readonly string _password;
        private readonly string _hmac;
        private readonly string _keyId;
        private readonly bool _isDemo;
        private readonly int _amountToValidateCreditCard;

        private readonly HashSet<string> _doNotHonorCodes = new HashSet<string>(new[] { "530", "606", "303" });

        private readonly IEncryptionService _encryptionService;
        private readonly ILogger _logger;

        public PaymentGateway(IEncryptionService encryptionService,
            IFirstDataService config,
            ILogger<PaymentGateway> logger)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            _encryptionService = encryptionService;

            _gatewayId = config.GetUsername();
            _password = config.GetPassword();
            _hmac = config.GetHmac();
            _keyId = config.GetKeyId();
            _isDemo = config.GetIsDemo();
            _amountToValidateCreditCard = config.GetAmountToValidate();
            _orderService = new ServiceSoapClient();
            _orderService.Endpoint.Name = _isDemo ? "ServiceSoapDemo" : "ServiceSoap";
            _orderService.Endpoint.Address = _isDemo ? new EndpointAddress(config.GetFirstDataServiceSoapDemo()) : new EndpointAddress(config.GetFirstDataServiceSoap());
            _orderService.ChannelFactory.Endpoint.EndpointBehaviors.Add(new HmacHeaderBehavior(_hmac, _keyId));

            _logger = logger;
        }

        private async Task<string> PostRequest(Transaction txn, int clientId, string typePlan)
        {
            PaymentErrorCode errorCode;
            try
            {
                txn.ExactID = _gatewayId;
                txn.Password = _password;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                SendAndCommitResponse response = await _orderService.SendAndCommitAsync(txn);
                TransactionResult apiResponse = response.SendAndCommitResult;
                string authNumber = apiResponse.Authorization_Num;
                if (!apiResponse.Transaction_Approved)
                {
                    string errorMessage = "";
                    string approvalCode = apiResponse.EXact_Resp_Code;
                    if (approvalCode != ResponseTypes.NORMAL_TRANSACTION)
                    {
                        errorMessage = apiResponse.EXact_Message;
                        switch (approvalCode)
                        {
                            case ResponseTypes.DUPLICATE:
                                errorCode = PaymentErrorCode.DuplicatedPaymentTransaction;
                                break;
                            default:
                                errorCode = PaymentErrorCode.DeclinedPaymentTransaction;
                                break;
                        }
                    }
                    else if (apiResponse.Bank_Resp_Code != null && _doNotHonorCodes.Contains(apiResponse.Bank_Resp_Code))
                    {
                        errorMessage = apiResponse.Bank_Message + " [Bank]";
                        errorCode = PaymentErrorCode.DoNotHonorPaymentResponse;
                    }
                    else
                    {
                        errorMessage = apiResponse.Bank_Message + " [Bank]";
                        errorCode = PaymentErrorCode.DeclinedPaymentTransaction;
                    }

                    _logger.LogError(String.Format("First Data Error: Client Id: {0}, CVDCode: {1}, CVD_Presence_Ind: {2}", clientId, txn.CVDCode, txn.CVD_Presence_Ind));
                    _logger.LogError(String.Format("Response: CVV: {0}, ErrorCode:{1}, ErrorMessage: {2}", apiResponse.CVV2, errorCode, errorMessage));

                    throw new DopplerApplicationException(errorCode, errorMessage);
                }
                return authNumber;
            }
            catch (DopplerApplicationException ex)
            {
                _logger.LogError(ex.Message);
                throw ex;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new DopplerApplicationException(PaymentErrorCode.ClientPaymentTransactionError, ex.Message, ex);
            }
        }

        private Transaction CreateDirectPaymentRequest(string type, decimal chargeTotal, CreditCard creditCard, int clientId)
        {
            Transaction txn = new Transaction
            {
                Transaction_Type = type,
                Customer_Ref = clientId.ToString(),
                CardHoldersName = _encryptionService.DecryptAES256(creditCard.HolderName),
                Card_Number = _encryptionService.DecryptAES256(creditCard.Number),
                Expiry_Date = String.Format("{0:00}{1:00}", creditCard.ExpirationMonth, creditCard.ExpirationYear % 100),
                DollarAmount = chargeTotal.ToString(CultureInfo.InvariantCulture),
                Reference_No = "Doppler Email Marketing"
            };

            if (creditCard.Code != null)
            {
                txn.CVD_Presence_Ind = "1";
                txn.CVDCode = _encryptionService.DecryptAES256(creditCard.Code);
            }

            return txn;
        }

        public async Task<bool> IsValidCreditCard(CreditCard creditCard, int clientId)
        {
            try
            {
                var paymentRequest = CreateDirectPaymentRequest(TransactionTypes.PRE_AUTH, _amountToValidateCreditCard, creditCard, clientId);
                await PostRequest(paymentRequest, clientId, null);
                return true;
            }
            catch (DopplerApplicationException ex)
            {
                switch (ex.ErrorCode)
                {
                    case PaymentErrorCode.DeclinedPaymentTransaction:
                    case PaymentErrorCode.DuplicatedPaymentTransaction:
                    case PaymentErrorCode.FraudPaymentTransaction:
                    case PaymentErrorCode.DoNotHonorPaymentResponse:
                        return false;
                    default:
                        throw;
                }
            }
        }
    }

    class HmacHeaderBehavior : IEndpointBehavior
    {
        private readonly string _hmac;
        private readonly string _keyId;

        public HmacHeaderBehavior(string hmac, string keyId)
        {
            _hmac = hmac;
            _keyId = keyId;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new HmacHeaderInspector(_hmac, _keyId));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }

    class HmacHeaderInspector : IClientMessageInspector
    {
        private readonly string _hmac;
        private readonly string _keyId;

        private const string Type = "text/xml; charset=utf-8";
        private const string Uri = "/transaction/v29";

        public HmacHeaderInspector(string hmac, string keyId)
        {
            _hmac = hmac;
            _keyId = keyId;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            MessageBuffer buffer = request.CreateBufferedCopy(Int32.MaxValue);
            request = buffer.CreateMessage();
            Message msg = buffer.CreateMessage();
            ASCIIEncoding encoder = new ASCIIEncoding();

            var sb = new StringBuilder();
            var xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings
            {
                OmitXmlDeclaration = true
            });
            var writer = XmlDictionaryWriter.CreateDictionaryWriter(xmlWriter);
            msg.WriteStartEnvelope(writer);
            msg.WriteStartBody(writer);
            msg.WriteBodyContents(writer);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            writer.Flush();

            string body = sb.ToString().Replace(" />", "/>");

            var contentDigest = GetHashedContent(body);

            var timeString = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var base64HmacSignature = GetHmacSignature(contentDigest, timeString);

            HttpRequestMessageProperty httpRequestMessageProperty;
            object httpRequestMessageObject;

            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out httpRequestMessageObject))
            {
                httpRequestMessageProperty = httpRequestMessageObject as HttpRequestMessageProperty;
                httpRequestMessageProperty.Headers["x-gge4-content-sha1"] = contentDigest;
                httpRequestMessageProperty.Headers["x-gge4-date"] = timeString;
                httpRequestMessageProperty.Headers["Authorization"] = string.Format("GGE4_API {0}:{1}", _keyId, base64HmacSignature);
            }
            else
            {
                httpRequestMessageProperty = new HttpRequestMessageProperty();
                httpRequestMessageProperty.Headers["x-gge4-content-sha1"] = contentDigest;
                httpRequestMessageProperty.Headers["x-gge4-date"] = timeString;
                httpRequestMessageProperty.Headers["Authorization"] = string.Format("GGE4_API {0}:{1}", _keyId, base64HmacSignature);
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessageProperty);
            }

            return null;
        }

        private string GetHashedContent(string payload)
        {
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var sha1_crypto = new SHA1CryptoServiceProvider();
            var hash = BitConverter.ToString(sha1_crypto.ComputeHash(payloadBytes)).Replace("-", "");
            return hash.ToLower();
        }

        private string GetHmacSignature(string hashedContent, string timeString)
        {
            var hashData = string.Format("POST\n{0}\n{1}\n{2}\n{3}", Type, hashedContent, timeString, Uri);

            var hmac_sha1 = new HMACSHA1(Encoding.UTF8.GetBytes(_hmac));
            var hmac_data = hmac_sha1.ComputeHash(Encoding.UTF8.GetBytes(hashData));

            return Convert.ToBase64String(hmac_data);
        }
    }
}
