using Prometheus;
using System.Diagnostics;

namespace Universalis.Application.Common;

public class UserAgentMetrics
{
    protected static readonly Counter UserAgentRequestCount =
        Metrics.CreateCounter("universalis_request_count_user_agents", "", "Controller", "Family");

    // For some reason user agents replace spaces with pluses sometimes
    private static readonly char[] UASegmentSeparators = { ' ', '+' };

    public static void RecordUserAgentRequest(string userAgent, string controllerName, Activity activity = null)
    {
        var controllerMetricName = GetControllerMetricName(controllerName);
        if (!string.IsNullOrEmpty(userAgent))
        {
            var firstSep = userAgent.IndexOfAny(UASegmentSeparators);
            var inferredUserAgentFriendlyName = firstSep != -1 ? userAgent[..(firstSep + 1)] : userAgent;
            activity?.AddTag("userAgent", inferredUserAgentFriendlyName);
            UserAgentRequestCount.Labels(controllerMetricName, inferredUserAgentFriendlyName).Inc();
        }
        else
        {
            activity?.AddTag("userAgent", "(no user agent)");
            UserAgentRequestCount.Labels(controllerMetricName, "(no user agent)").Inc();
        }
    }

    private static string GetControllerMetricName(string controllerName)
    {
        const string suffix = "Controller";
        if (controllerName.EndsWith(suffix))
        {
            return controllerName[..^suffix.Length];
        }

        return controllerName;
    }
}