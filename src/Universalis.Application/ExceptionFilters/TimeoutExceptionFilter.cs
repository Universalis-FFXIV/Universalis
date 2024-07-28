using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;

namespace Universalis.Application.ExceptionFilters;

public class TimeoutExceptionFilter : IExceptionFilter
{
    private readonly ILogger _logger;

    public TimeoutExceptionFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TimeoutExceptionFilter>();
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not TimeoutException) return;
        _logger.LogWarning("Request timed out");
        context.Result = new StatusCodeResult(504);
        context.ExceptionHandled = true;
        context.HttpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
    }
}
