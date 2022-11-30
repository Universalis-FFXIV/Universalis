using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Universalis.Application.ExceptionFilters;

public class RedisTimeoutExceptionFilter : IExceptionFilter
{
    private readonly ILogger _logger;

    public RedisTimeoutExceptionFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RedisTimeoutExceptionFilter>();
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not RedisTimeoutException) return;
        _logger.LogWarning("Redis operation timed out");
        context.Result = new StatusCodeResult(504);
    }
}
