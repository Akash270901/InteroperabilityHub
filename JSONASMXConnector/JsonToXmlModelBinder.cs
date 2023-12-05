using Newtonsoft.Json;
using System.IO;
using System.Web.Mvc;
using System.Xml.Linq;

namespace JSONASMXConnector
{
    public class JsonToXmlModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var request = controllerContext.HttpContext.Request;

            using (var reader = new StreamReader(request.InputStream))
            {
                var jsonString = reader.ReadToEnd();

                // Convert JSON to XML
                var jsonDoc = JsonConvert.DeserializeXmlNode(jsonString, "Root");

                return jsonDoc;
            }
        }
    }
}