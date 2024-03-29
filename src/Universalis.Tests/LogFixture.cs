﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Universalis.Tests;

public class LogFixture<T> : ILogger<T>
{
    // ReSharper disable once CollectionNeverQueried.Local
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

    // ReSharper disable once UnusedTypeParameter
    internal class LogFixtureScope<TState> : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
