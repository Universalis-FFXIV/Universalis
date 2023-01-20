using Microsoft.Extensions.Logging;
using System;

namespace Universalis.Tests;

public class LogEvent
{
    public LogLevel Level { get; set; }

    public string Message { get; set; }

    public Exception Exception { get; set; }

    public object State { get; set; }
}
