using JSONASMXConnector.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace JSONASMXConnector.CustomModules
{
    /// <summary>
    /// Custom HTTP module for processing requests.
    /// </summary>
    public class CustomHttpModule : IHttpModule
    {
        private static readonly object lockObject = new object();

        /// <summary>
        /// Initialize the HTTP module.
        /// </summary>
        /// <param name="context">The HttpApplication instance.</param>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
        }

        public void Dispose() { }

        /// <summary>
        /// Event handler for incoming requests.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnBeginRequest(object sender, EventArgs e)
        {
            // Ensure thread safety with a lock
            lock (lockObject)
            {
                var application = (HttpApplication)sender;
                var httpResponse = application.Response;

                // Add security headers
                httpResponse.AddHeader("Content-Security-Policy", "default-src 'self'");
                httpResponse.AddHeader("X-Content-Type-Options", "nosniff");
                httpResponse.AddHeader("X-Frame-Options", "DENY");
                httpResponse.AddHeader("X-XSS-Protection", "1; mode=block");

                var request = application.Request;
                string endpoint = HttpContext.Current.Request.Path;

                // Skip processing for initialization requests
                if (IsProjectInitializationRequest(endpoint))
                    return;

                bool IsAuthorized = false;
                string authorizationToken = request.Headers["Authorization"];
                string ipAddress = request.UserHostAddress;

                int lastSlashIndex = endpoint.LastIndexOf('/');
                string api_ver = string.Empty;
                if (lastSlashIndex > 0)
                {
                    api_ver = endpoint.Substring(0, lastSlashIndex);
                    endpoint = endpoint.Split('/').Last();
                }
                else
                {
                    api_ver = "/v5_0/API";
                    endpoint = char.ToUpper(endpoint[1]) + endpoint.Substring(2);
                }

                string token1 = ConfigurationManager.AppSettings["token1"];
                string path = System.Web.Hosting.HostingEnvironment.MapPath($"{ConfigurationManager.AppSettings["loggingPath"]}/{api_ver}");
                string serviceurl = ConfigurationManager.AppSettings["Serviceurl"];
                serviceurl += api_ver + ".asmx";
                HttpWebRequest newrequest = (HttpWebRequest)WebRequest.Create(serviceurl);
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    if (token1 == authorizationToken)
                        IsAuthorized = true;

                    // Process the request body
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

                        // Deserialize JSON body to JObject
                        JObject jObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(Body);
                        ModifyNullValues(jObject);
                        var xmlBody = ConvertJObjectToXml(jObject);

                        // Convert JSON to SOAP XML
                        string soapXml = ConvertToSoapXml(xmlBody, endpoint, api_ver);
                        xmlDoc.LoadXml(soapXml);

                        // Set up HTTP request
                        newrequest.Method = request.HttpMethod;
                        newrequest.ContentType = "application/soap+xml";

                        // Write SOAP XML to request stream
                        using (StreamWriter writer = new StreamWriter(newrequest.GetRequestStream()))
                        {
                            writer.Write(xmlDoc.InnerXml);
                        }

                        // Get response from the service
                        try
                        {
                            using (HttpWebResponse response = (HttpWebResponse)newrequest.GetResponse())
                            {
                                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                                {
                                    string xmlResponse = reader.ReadToEnd();
                                    var xDoc = XDocument.Parse(xmlResponse);

                                    // Convert XML response to JSON
                                    var converter = new Newtonsoft.Json.Converters.XmlNodeConverter { OmitRootObject = true };
                                    var rootToken = JObject.FromObject(xDoc, Newtonsoft.Json.JsonSerializer.CreateDefault(new Newtonsoft.Json.JsonSerializerSettings { Converters = { converter } }))
                                        .ReplaceXmlNilObjectsWithNull();

                                    var resultEnvelope = rootToken["soap:Envelope"];
                                    var bodyResponse = rootToken["soap:Body"];
                                    JObject finalObj;

                                    // Extract the specific result from the SOAP response
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
                                    var jsonRequestDetails = new
                                    {
                                        Request = new
                                        {
                                            request.HttpMethod,
                                            request.ContentType,
                                            Endpoint = endpoint,
                                            AuthorizationToken = authorizationToken
                                        },
                                        Client = new
                                        {
                                            IPAddress = ipAddress
                                        },
                                        Time = DateTime.UtcNow,
                                        Response = new
                                        {
                                            finalObj
                                        },
                                        Authorized = IsAuthorized
                                    };
                                    if (!Directory.Exists(path))
                                    {
                                        Directory.CreateDirectory(path);
                                    }
                                    string formattedDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd-hh-mm-ss");

                                    string xmlLogPath = Path.Combine(path, $"{formattedDateTime}-{endpoint}.xml");

                                    // Log the XML request and response
                                    using (XmlWriter writer = XmlWriter.Create(xmlLogPath))
                                    {
                                        writer.WriteStartElement($"{endpoint}");
                                        writer.WriteElementString("FullRequest", xmlDoc.InnerXml.ToString());
                                        writer.WriteStartElement("Request");
                                        writer.WriteElementString("HttpMethod", newrequest.Method);
                                        writer.WriteElementString("ContentType", newrequest.ContentType);
                                        writer.WriteElementString("Endpoint", endpoint);
                                        writer.WriteElementString("AuthorizationToken", authorizationToken);
                                        writer.WriteEndElement();
                                        writer.WriteStartElement("Client");
                                        writer.WriteElementString("IPAddress", ipAddress);
                                        writer.WriteEndElement();
                                        writer.WriteElementString("Time", DateTime.UtcNow.ToString());
                                        writer.WriteElementString("Response", xmlResponse);
                                        writer.WriteElementString("Authorized", IsAuthorized.ToString());
                                        writer.WriteEndElement();
                                    }

                                    // Serialize JSON request details and log as JSON
                                    string jsondata = Newtonsoft.Json.JsonConvert.SerializeObject(jsonRequestDetails, Newtonsoft.Json.Formatting.Indented);
                                    string jsonLogPath = Path.Combine(path, $"{formattedDateTime}-{endpoint}.json");
                                    File.WriteAllText(jsonLogPath, jsondata);
                                    ClearAndWriteJsonResponse(application, finalResult);
                                }
                            }
                        }
                        catch (WebException webEx)
                        {
                            // If the server returns an error, read the error response
                            string errorResponseText = string.Empty;
                            if (webEx.Response != null)
                            {
                                using (var errorResponse = (HttpWebResponse)webEx.Response)
                                using (var errorReader = new StreamReader(errorResponse.GetResponseStream()))
                                {
                                    var errorText = errorReader.ReadToEnd();
                                    string pattern = @"System\.Exception:\s*\d+:\s*([^\.]+)\.";
                                    Match match = Regex.Match(errorText, pattern);
                                    if (match.Success)
                                    {
                                        errorResponseText = match.Groups[1].Value;
                                    }
                                }
                            }
                            else
                                errorResponseText = webEx.Message;

                            throw new Exception(errorResponseText, webEx);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Create JSON response for the exception
                    var jsonRequestDetails = new
                    {
                        Request = new
                        {
                            request.HttpMethod,
                            request.ContentType,
                            Endpoint = endpoint,
                            AuthorizationToken = authorizationToken
                        },
                        Client = new
                        {
                            IPAddress = ipAddress
                        },
                        Time = DateTime.UtcNow,
                        Error = new
                        {
                            ex.Message,
                            ex.StackTrace
                        },
                        Authorized = IsAuthorized
                    };
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    string formattedDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd-hh-mm-ss");

                    string xmlLogFileName = $"{formattedDateTime}-{endpoint}.xml";
                    string xmlLogPath = Path.Combine(path, xmlLogFileName);

                    // Log the XML request and error details
                    using (XmlWriter writer = XmlWriter.Create(xmlLogPath))
                    {
                        writer.WriteStartElement($"{endpoint}");
                        writer.WriteStartElement("Request");
                        writer.WriteElementString("FullRequest", xmlDoc.InnerXml.ToString());
                        writer.WriteElementString("HttpMethod", newrequest.Method);
                        writer.WriteElementString("ContentType", newrequest.ContentType);
                        writer.WriteElementString("Endpoint", endpoint);
                        writer.WriteElementString("AuthorizationToken", authorizationToken);
                        writer.WriteEndElement();
                        writer.WriteStartElement("Client");
                        writer.WriteElementString("IPAddress", ipAddress);
                        writer.WriteEndElement();
                        writer.WriteElementString("Time", DateTime.UtcNow.ToString());
                        writer.WriteStartElement("Error");
                        writer.WriteElementString("Message", ex.Message);
                        writer.WriteElementString("StackTrace", ex.StackTrace);
                        writer.WriteEndElement();
                        writer.WriteElementString("Authorized", IsAuthorized.ToString());
                        writer.WriteEndElement();
                    }

                    // Serialize JSON request details and log as JSON
                    string jsondata = Newtonsoft.Json.JsonConvert.SerializeObject(jsonRequestDetails, Newtonsoft.Json.Formatting.Indented);
                    string jsonLogPath = Path.Combine(path, $"{formattedDateTime}-{endpoint}.json");
                    File.WriteAllText(jsonLogPath, jsondata);

                    // Respond to the client with the error message
                    ClearAndWriteJsonResponse(application, "Updated");
                }
            }
        }

        private void ClearAndWriteJsonResponse(HttpApplication application, dynamic bodyResponse)
        {
            // Clear the existing response, set content type to JSON, and write the response body
            application.Response.Clear();
            application.Response.ContentType = "application/json";
            application.Response.Write(bodyResponse);
            application.CompleteRequest();
        }

        private string ConvertToSoapXml(string xmlBody, string endpoint, string api_ver)
        {
            string serviceXmlns = ConfigurationManager.AppSettings[$"{api_ver}"];

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
        private bool IsProjectInitializationRequest(string endpoint)
        {
            // Skip requests to initialization URLs
            return endpoint.Equals("/", StringComparison.OrdinalIgnoreCase) ||
                   endpoint.Equals("/index.html", StringComparison.OrdinalIgnoreCase) ||
                   endpoint.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
        }
        private void OnEndRequest(object sender, EventArgs e)
        {
        }
    }
}