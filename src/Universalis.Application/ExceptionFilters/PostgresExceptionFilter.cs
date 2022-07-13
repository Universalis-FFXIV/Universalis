using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Universalis.Application.ExceptionFilters;

public class PostgresExceptionFilter : IExceptionFilter
{
    private readonly ILogger _logger;

    public PostgresExceptionFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PostgresExceptionFilter>();
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not PostgresException postgresException) return;
        _logger.LogInformation("Postgres driver raised an exception: {Message}\n\tException source: {File}:{Line}",
            postgresException.MessageText, postgresException.File, postgresException.Line);
        context.ExceptionHandled = true;
        context.Result = new StatusCodeResult(500);
    }
}