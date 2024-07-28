using Cassandra;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Universalis.Application.ExceptionFilters;

public class ReadTimeoutExceptionFilter : IExceptionFilter
{
    private readonly ILogger _logger;

    public ReadTimeoutExceptionFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ReadTimeoutExceptionFilter>();
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ReadTimeoutException) return;
        _logger.LogWarning(context.Exception, "Cassandra read request was cancelled");
        context.Result = new StatusCodeResult(504);
        context.ExceptionHandled = true;
        context.HttpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
    }
}
