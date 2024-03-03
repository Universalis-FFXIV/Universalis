using Cassandra.Metrics.Abstractions;
using Prometheus;

namespace Universalis.DbAccess.Metrics;

public class PrometheusDataStaxMeter : IDriverMeter
{
    private readonly Gauge _gauge;

    private readonly string _bucket;

    public PrometheusDataStaxMeter(string name, string bucket)
    {
        _bucket = bucket;
        _gauge = Prometheus.Metrics.CreateGauge(name, "", "bucket");
    }

    public void Mark(long amount)
    {
        _gauge.WithLabels(_bucket).Set(amount);
    }
}