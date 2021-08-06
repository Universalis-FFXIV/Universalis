using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Universalis.Application.Controllers
{
    public class DecoderFallbackExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is DecoderFallbackException)
            {
                // It may be insecure to just dump in the exception message, so we'll
                // just assume it's always something like this... hopefully.
                context.Result = new BadRequestObjectResult(
                    "Unable to translate bytes from specified code page to Unicode.");
            }
        }
    }
}