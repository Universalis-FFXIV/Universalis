using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;

namespace Universalis.Application.ExceptionFilters;

public class InvalidOperationExceptionFilter : IExceptionFilter
{
    private readonly ILogger _logger;

    public InvalidOperationExceptionFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OperationCancelledExceptionFilter>();
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not InvalidOperationException) return;
        _logger.LogInformation("Request failed");
        context.ExceptionHandled = true;
        context.Result = new StatusCodeResult(500);
    }
}
