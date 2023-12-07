using JSONASMXConnector.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace JSONASMXConnector.CustomModules
{
    public class CustomHttpModule : IHttpModule
    {
        private static bool applicationStarted = false;
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
        }

        public void Dispose()
        {
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            if (!applicationStarted)
            {
                applicationStarted = true;
                return;
            }
            var application = (HttpApplication)sender;
            try
            {
                var request = application.Request;
                string endpoint = HttpContext.Current.Request.Path;
                endpoint = char.ToUpper(endpoint[1]) + endpoint.Substring(2);
                using (var reqbody = new StreamReader(request.InputStream))
                {
                    var Body = reqbody.ReadToEnd();
                    if (Body == string.Empty)
                    {
                        ClearAndWriteJsonResponse(application, ConfigurationManager.AppSettings["Error:EmptyBody"]);
                        return;
                    }
                    if (request.ContentType != "application/json")
                    {
                        ClearAndWriteJsonResponse(application, ConfigurationManager.AppSettings["Error:ContentType"]);
                        return;
                    }
                    JObject jObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(Body);
                    ModifyNullValues(jObject);
                    var xmlBody = ConvertJObjectToXml(jObject);

                    string soapXml = ConvertToSoapXml(xmlBody, endpoint);
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(soapXml);
                    string serviceurl = ConfigurationManager.AppSettings["Serviceurl"];
                    HttpWebRequest newrequest = (HttpWebRequest)WebRequest.Create(serviceurl);
                    newrequest.Method = request.HttpMethod;
                    newrequest.ContentType = "application/soap+xml";
                    using (StreamWriter writer = new StreamWriter(newrequest.GetRequestStream()))
                    {
                        writer.Write(xmlDoc.InnerXml);
                    }
                    using (HttpWebResponse response = (HttpWebResponse)newrequest.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            string xmlResponse = reader.ReadToEnd();
                            var xDoc = XDocument.Parse(xmlResponse);

                            var converter = new Newtonsoft.Json.Converters.XmlNodeConverter { OmitRootObject = true };
                            var rootToken = JObject.FromObject(xDoc, Newtonsoft.Json.JsonSerializer.CreateDefault(new Newtonsoft.Json.JsonSerializerSettings { Converters = { converter } }))
                                .ReplaceXmlNilObjectsWithNull();

                            var resultEnvelope = rootToken["soap:Envelope"];
                            var bodyResponse = rootToken["soap:Body"];
                            JObject finalObj;
                            var result = bodyResponse[$"{endpoint}Response"][$"{endpoint}Result"];
                            if (result != null && result.Type == JTokenType.Object)
                            {
                                finalObj = (JObject)result;
                            }
                            else
                            {
                                finalObj = new JObject();
                            }
                            string finalResult = finalObj.ToString();
                            ClearAndWriteJsonResponse(application, finalResult);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ClearAndWriteJsonResponse(application, ex.Message.ToString());
            }
        }

        private void ClearAndWriteJsonResponse(HttpApplication application, dynamic bodyResponse)
        {
            application.Response.Clear();
            application.Response.ContentType = "application/json";
            application.Response.Write(bodyResponse);
            application.CompleteRequest();
        }

        private string ConvertToSoapXml(string xmlBody, string endpoint)
        {
            string xmlns = ConfigurationManager.AppSettings["xmlns"];
            string serviceXmlns = ConfigurationManager.AppSettings["ServiceXmlns"];

            var soapXml = $@"<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Body>
    <{endpoint} xmlns=""{serviceXmlns}"">
      {xmlBody}
    </{endpoint}>
  </soap12:Body>
</soap12:Envelope>
";
            return soapXml;
        }

        private string ConvertJObjectToXml(JObject jObject)
        {
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = new XmlTextWriter(stringWriter))
            {
                xmlWriter.Formatting = (Formatting)Newtonsoft.Json.Formatting.Indented;
                WriteXml(jObject, xmlWriter);
                return stringWriter.ToString();
            }
        }

        private void WriteXml(JToken token, XmlWriter writer)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                writer.WriteValue(string.Empty);
                return;
            }

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

        private void ModifyNullValues(JObject jObject)
        {
            foreach (var property in jObject.Properties())
            {
                if (property.Value.Type == JTokenType.Null)
                {
                    property.Value = new JValue("null");
                }
            }
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
        }
    }
}