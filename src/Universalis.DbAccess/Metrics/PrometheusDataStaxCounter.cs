using Cassandra.Metrics.Abstractions;
using Prometheus;

namespace Universalis.DbAccess.Metrics;

public class PrometheusDataStaxCounter : IDriverCounter
{
    private readonly Counter _counter;

    private readonly string _bucket;

    public PrometheusDataStaxCounter(string name, string bucket)
    {
        _bucket = bucket;
        _counter = Prometheus.Metrics.CreateCounter(name, "", "bucket");
    }

    public void Increment()
    {
        _counter.WithLabels(_bucket).Inc();
    }

    public void Increment(long value)
    {
        _counter.WithLabels(_bucket).Inc(value);
    }
}