using System.Web.Http;
using System.Web;
using System.Xml;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Configuration;
using System.Text;

namespace JSONASMXConnector.Controllers
{
    public class QuotingApiController : ApiController
    {
        public QuotingApiController()
        {

        }
        //[Route("generateQuotation")]
        //[Route("findQuotation")]
        public IHttpActionResult GenerateQuotation(string username, string apiKey, int? quotationId, [FromBody] object obj)
        {
            return Ok();
            //string soapRequest;
            //string endpoint = HttpContext.Current.Request.Path;
            //var soapEnvelope = ConfigurationManager.AppSettings["SoapEnvelope"];
            //string credentials = GenerateXml(("Username", username), ("ApiKey", apiKey));
            //if (endpoint == "/findQuotation")
            //{
            //    var reqfindQuotation = ConfigurationManager.AppSettings["ReqfindQuotation"];
            //    string xmlQuotationId = GenerateXml(("QuotationId", quotationId));
            //    string findQuotationEnd = ConfigurationManager.AppSettings["FindQuotationEnd"];
            //    //var soapXml = "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">\r\n  <soap12:Body>\r\n    <GenerateQuotation xmlns=\"com.kinnerlsey.dummy.api.quoting.v5_0\">\r\n      <Username>string</Username>\r\n      <ApiKey>string</ApiKey>\r\n      <circuit>\r\n        <ProductType>None or RemoveEmptyEntries</ProductType>\r\n        <CustomerId>8</CustomerId>\r\n        <CRMReference>string</CRMReference>\r\n        <AEndNNIId>8</AEndNNIId>\r\n        <ShadowVlanNNIId>8</ShadowVlanNNIId>\r\n        <AEndPostcode>string</AEndPostcode>\r\n        <BEndPostcode>string</BEndPostcode>\r\n        <PortAndBandwidths>\r\n          <PortAndBandwidth>\r\n            <AEndPort>string</AEndPort>\r\n            <BEndPort>string</BEndPort>\r\n            <Bandwidth>string</Bandwidth>\r\n          </PortAndBandwidth>\r\n          <PortAndBandwidth>\r\n            <AEndPort>string</AEndPort>\r\n            <BEndPort>string</BEndPort>\r\n            <Bandwidth>string</Bandwidth>\r\n          </PortAndBandwidth>\r\n        </PortAndBandwidths>\r\n        <TermLengthInMonths>\r\n          <int>8</int>\r\n          <int>8</int>\r\n        </TermLengthInMonths>\r\n        <AEndFloor>8</AEndFloor>\r\n        <BEndFloor>8</BEndFloor>\r\n        <AEndPoPId>8</AEndPoPId>\r\n        <BEndPoPId>8</BEndPoPId>\r\n        <AEndLocationIdentifiers>\r\n          <LocationIdentifier>\r\n            <Identifier>string</Identifier>\r\n            <Descriptor>string</Descriptor>\r\n          </LocationIdentifier>\r\n          <LocationIdentifier>\r\n            <Identifier>string</Identifier>\r\n            <Descriptor>string</Descriptor>\r\n          </LocationIdentifier>\r\n        </AEndLocationIdentifiers>\r\n        <BEndLocationIdentifiers>\r\n          <LocationIdentifier>\r\n            <Identifier>string</Identifier>\r\n            <Descriptor>string</Descriptor>\r\n          </LocationIdentifier>\r\n          <LocationIdentifier>\r\n            <Identifier>string</Identifier>\r\n            <Descriptor>string</Descriptor>\r\n          </LocationIdentifier>\r\n        </BEndLocationIdentifiers>\r\n        <AEndExcludeNeosOnnet>boolean</AEndExcludeNeosOnnet>\r\n        <BEndExcludeNeosOnnet>boolean</BEndExcludeNeosOnnet>\r\n        <IsDiverse>boolean</IsDiverse>\r\n        <IsManagedDIA>boolean</IsManagedDIA>\r\n        <InstallAmortisation>boolean</InstallAmortisation>\r\n        <ChosenAccessTypes>\r\n          <string>string</string>\r\n          <string>string</string>\r\n        </ChosenAccessTypes>\r\n        <AdditionalServices>\r\n          <string>string</string>\r\n          <string>string</string>\r\n        </AdditionalServices>\r\n        <CloudConnectOptions>\r\n          <string>string</string>\r\n          <string>string</string>\r\n        </CloudConnectOptions>\r\n      </circuit>\r\n    </GenerateQuotation>\r\n  </soap12:Body>\r\n</soap12:Envelope>";
            //    soapRequest = soapEnvelope + reqfindQuotation + credentials + xmlQuotationId + findQuotationEnd;
            //    //soapRequest = soapXml;
            //}
            //else
            //{
            //    if (obj is JObject jObject)
            //    {
            //        string reqGenerateQuotation = ConfigurationManager.AppSettings["ReqGenerateQuotation"];
            //        string xmlObj = ConvertJObjectToXml(jObject);
            //        soapRequest = soapEnvelope + reqGenerateQuotation + xmlObj;
            //    }
            //    else
            //    {
            //        return BadRequest("Invalid object type. Expected JObject.");
            //    }
            //}
            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.LoadXml(soapRequest);
            //string serviceurl = ConfigurationManager.AppSettings["Serviceurl"];
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceurl);
            //request.Method = "POST";
            //request.ContentType = "text/xml";
            //using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
            //{
            //    writer.Write(xmlDoc.InnerXml);
            //}
            //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            //{
            //    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            //    {
            //        string xmlResponse = reader.ReadToEnd();
            //        return Ok(xmlResponse);
            //    }
            //}
        }
        private string ConvertJObjectToXml(JObject jObject)
        {
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = new XmlTextWriter(stringWriter))
            {
                xmlWriter.Formatting = Formatting.Indented;

                WriteXml(jObject, xmlWriter);

                return stringWriter.ToString();
            }
        }

        private void WriteXml(JToken token, XmlWriter writer)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var property in ((JObject)token).Properties())
                    {
                        writer.WriteStartElement(property.Name);
                        WriteXml(property.Value, writer);
                        writer.WriteEndElement();
                    }
                    break;

                case JTokenType.Array:
                    foreach (var item in (JArray)token)
                    {
                        WriteXml(item, writer);
                    }
                    break;

                case JTokenType.Property:
                    var prop = (JProperty)token;
                    writer.WriteStartElement(prop.Name);
                    WriteXml(prop.Value, writer);
                    writer.WriteEndElement();
                    break;

                default:
                    writer.WriteValue(((JValue)token).Value);
                    break;
            }
        }

        private string GenerateXml(params (string paramName, dynamic paramValue)[] parameters)
        {
            StringBuilder xmlBuilder = new StringBuilder();
            foreach (var (paramName, paramValue) in parameters)
            {
                xmlBuilder.Append($"<{paramName}>{paramValue}</{paramName}>\n");
            }
            return xmlBuilder.ToString();
        }
    }
}