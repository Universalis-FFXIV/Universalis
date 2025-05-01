using System.Diagnostics;
using JetBrains.Annotations;

namespace Universalis.Application.Common.Metrics;

public class IPTrace
{
    /// <summary>
    /// Records the connecting IP address to the provided tracing context.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="activity">The trace context.</param>
    public static void RecordConnectingIP(string ipAddress, [CanBeNull] Activity activity)
    {
        activity?.AddTag("ipAddress", ipAddress);
    }
}