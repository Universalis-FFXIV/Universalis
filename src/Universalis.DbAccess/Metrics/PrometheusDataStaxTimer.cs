using Cassandra.Metrics.Abstractions;
using Prometheus;

namespace Universalis.DbAccess.Metrics;

public class PrometheusDataStaxTimer : IDriverTimer
{
    private readonly Histogram _histogram;

    private readonly string _bucket;

    public PrometheusDataStaxTimer(string name, string bucket)
    {
        _bucket = bucket;
        _histogram = Prometheus.Metrics.CreateHistogram(name, "A timer measuring data in nanoseconds.",
            new HistogramConfiguration
            {
                LabelNames = new[] { "bucket" },
                Buckets = Histogram.ExponentialBuckets(1, 2, 16),
            });
    }

    public void Record(long elapsedNanoseconds)
    {
        _histogram.WithLabels(_bucket).Observe(elapsedNanoseconds);
    }
}