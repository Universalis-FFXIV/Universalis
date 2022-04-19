using System;
using Microsoft.Extensions.Logging;

namespace Universalis.Application.Realtime;

public class LoggerShield<TCategory> : ILogger<TCategory>
{
    private readonly ILogger<TCategory> _logger;
    private readonly Guid _id;

    public LoggerShield(ILogger<TCategory> logger, Guid id)
    {
        _logger = logger;
        _id = id;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, (s, e) =>
        {
            var message = formatter(s, e);
            return $"({_id}) {message}";
        });
    }
}