using System;
using System.Globalization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public class HmacHeaderBehavior : IEndpointBehavior
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

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) => clientRuntime.ClientMessageInspectors.Add(new HmacHeaderInspector(_hmac, _keyId));

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }

    internal class HmacHeaderInspector : IClientMessageInspector
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
            var buffer = request.CreateBufferedCopy(int.MaxValue);
            request = buffer.CreateMessage();
            var msg = buffer.CreateMessage();

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

            var body = sb.ToString().Replace(" />", "/>");

            var contentDigest = GetHashedContent(body);

            var timeString = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            var base64HmacSignature = GetHmacSignature(contentDigest, timeString);

            HttpRequestMessageProperty httpRequestMessageProperty;

            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out var httpRequestMessageObject))
            {
                httpRequestMessageProperty = httpRequestMessageObject as HttpRequestMessageProperty;
                httpRequestMessageProperty.Headers["x-gge4-content-sha1"] = contentDigest;
                httpRequestMessageProperty.Headers["x-gge4-date"] = timeString;
                httpRequestMessageProperty.Headers["Authorization"] = string.Format(CultureInfo.InvariantCulture, "GGE4_API {0}:{1}", _keyId, base64HmacSignature);
            }
            else
            {
                httpRequestMessageProperty = new HttpRequestMessageProperty();
                httpRequestMessageProperty.Headers["x-gge4-content-sha1"] = contentDigest;
                httpRequestMessageProperty.Headers["x-gge4-date"] = timeString;
                httpRequestMessageProperty.Headers["Authorization"] = string.Format(CultureInfo.InvariantCulture, "GGE4_API {0}:{1}", _keyId, base64HmacSignature);
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessageProperty);
            }

            return null;
        }

        private static string GetHashedContent(string payload)
        {
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var sha1_crypto = new SHA1CryptoServiceProvider();
            var hash = BitConverter.ToString(sha1_crypto.ComputeHash(payloadBytes)).Replace("-", "");
            return hash.ToLower(CultureInfo.InvariantCulture);
        }

        private string GetHmacSignature(string hashedContent, string timeString)
        {
            var hashData = string.Format(CultureInfo.InvariantCulture, "POST\n{0}\n{1}\n{2}\n{3}", Type, hashedContent, timeString, Uri);

            var hmac_sha1 = new HMACSHA1(Encoding.UTF8.GetBytes(_hmac));
            var hmac_data = hmac_sha1.ComputeHash(Encoding.UTF8.GetBytes(hashData));

            return Convert.ToBase64String(hmac_data);
        }
    }
}
