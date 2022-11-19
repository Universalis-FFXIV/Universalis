using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Universalis.Application.Tests;

internal class LogFixture<T> : ILogger<T>
{
    private readonly IList<LogEvent> _events = new List<LogEvent>();

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        lock (_events)
        {
            _events.Add(new LogEvent
            {
                Level = logLevel,
                Message = formatter(state, exception),
                Exception = exception,
                State = state,
            });
        }
    }

    internal class LogFixtureScope<TState> : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
