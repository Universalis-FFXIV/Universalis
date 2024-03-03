using Cassandra.Metrics.Abstractions;

namespace Universalis.DbAccess.Metrics;

public class PrometheusDataStaxGauge : IDriverGauge
{
    // How is this intended to be implemented?? Is the intention to just call the provider function yourself every now and then?
    // https://github.com/datastax/csharp-driver/blob/d1ab72a1d82e4645a981c3e3eca4ecd5d7b7f5a8/src/Extensions/Cassandra.AppMetrics/Implementations/AppMetricsDriverMetricsProvider.cs#L92
}