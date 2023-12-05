using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace JSONASMXConnector.Middlewares
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
                var queryParameters = request.QueryString;
                List<object> parameterKeys = new List<object>();
                string paramsXml = string.Empty;
                string xmlBody = string.Empty;
                string endpoint = HttpContext.Current.Request.Path;
                string funcName = ConfigurationManager.AppSettings[endpoint];
                if (queryParameters.Count > 0)
                {
                    foreach (var property in queryParameters.AllKeys)
                    {
                        var paramObj = new
                        {
                            key = property,
                            value = HttpContext.Current.Request.QueryString[property]
                        };
                        parameterKeys.Add(paramObj);
                    }
                    paramsXml = ConvertParamsToXml(parameterKeys);
                }
                using (var reqbody = new StreamReader(request.InputStream))
                {
                    var Body = reqbody.ReadToEnd();
                    if (Body != string.Empty)
                    {
                        if (request.ContentType == "application/json")
                        {
                            JObject jObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(Body);
                            ModifyNullValues(jObject);
                            var objXml = ConvertJObjectToXml(jObject);
                            xmlBody = $"<circuit>\r\n   " +
                                $"{objXml}\r\n   " +
                                $"</circuit>";
                        }
                    }
                    string soapXml = ConvertToSoapXml(paramsXml, xmlBody, funcName);
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(soapXml);
                    string serviceurl = ConfigurationManager.AppSettings["Serviceurl"];
                    HttpWebRequest newrequest = (HttpWebRequest)WebRequest.Create(serviceurl);
                    newrequest.Method = request.HttpMethod;
                    newrequest.ContentType = "application/soap+xml; charset=utf-8";
                    using (StreamWriter writer = new StreamWriter(newrequest.GetRequestStream()))
                    {
                        writer.Write(xmlDoc.InnerXml);
                    }
                    using (HttpWebResponse response = (HttpWebResponse)newrequest.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            string xmlResponse = reader.ReadToEnd();
                            XDocument responseXmlDoc = XDocument.Parse(xmlResponse);
                            string jsonString = Newtonsoft.Json.JsonConvert.SerializeXNode(responseXmlDoc);
                            var jsonResponse = JObject.Parse(jsonString);
                            var resultEnvelope = jsonResponse["soap:Envelope"];
                            var bodyResponse = resultEnvelope["soap:Body"];

                            ClearAndWriteJsonResponse(application, bodyResponse.ToString());
                        }
                    }
                }
            }
            catch(Exception ex)
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
        private string ConvertToSoapXml(string paramsXml, string body, string funcName)
        {
            string nsxsi = ConfigurationManager.AppSettings["nsxsi"];
            string nsxsd = ConfigurationManager.AppSettings["nsxsd"];
            string nssoap12 = ConfigurationManager.AppSettings["nssoap12"];
            string xmlns = ConfigurationManager.AppSettings["xmlns"];
            var soapXml = $"<soap12:Envelope xmlns:xsi=\"{nsxsi}\" xmlns:xsd=\"{nsxsd}\" xmlns:soap12=\"{nssoap12}\">\r\n  <soap12:Body>\r\n    <{funcName} xmlns=\"{xmlns}\">\r\n      {paramsXml}    {body}\r\n    </{funcName}>\r\n  </soap12:Body>\r\n</soap12:Envelope>";
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

        private string ConvertParamsToXml(List<object> paramsList)
        {
            StringBuilder xmlBuilder = new StringBuilder();
            foreach (var item in paramsList)
            {
                xmlBuilder.Append($"<{item.GetType().GetProperty("key").GetValue(item)}>{item.GetType().GetProperty("value").GetValue(item)}</{item.GetType().GetProperty("key").GetValue(item)}>\r\n   ");
            }
            return xmlBuilder.ToString();
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