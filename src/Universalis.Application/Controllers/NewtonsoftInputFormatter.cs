using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace Universalis.Application.Controllers
{
    public class NewtonsoftInputFormatter : TextInputFormatter
    {
        public NewtonsoftInputFormatter()
        {
            SupportedMediaTypes.Add("application/json");
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            using var bodyReader = new StreamReader(context.HttpContext.Request.Body);
            var body = await bodyReader.ReadToEndAsync();
            var bodyObject = JsonConvert.DeserializeObject(body);
            return bodyObject == null
                ? await InputFormatterResult.FailureAsync()
                : await InputFormatterResult.SuccessAsync(bodyObject);
        }
    }
}