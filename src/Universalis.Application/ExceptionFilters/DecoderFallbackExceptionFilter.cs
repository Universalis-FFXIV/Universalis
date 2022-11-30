using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Universalis.Application.ExceptionFilters;

public class DecoderFallbackExceptionFilter : IExceptionFilter
{
    private readonly ILogger _logger;

    public DecoderFallbackExceptionFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DecoderFallbackExceptionFilter>();
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DecoderFallbackException) return;
        _logger.LogWarning("Failed to translate bytes from specified code page to Unicode");
        // It may be insecure to just dump in the exception message, so we'll
        // just assume it's always something like this... hopefully.
        context.Result = new BadRequestObjectResult(
            "Unable to translate bytes from specified code page to Unicode.");
    }
}