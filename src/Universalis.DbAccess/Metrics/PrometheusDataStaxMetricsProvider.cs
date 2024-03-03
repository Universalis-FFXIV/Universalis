using System;
using Cassandra.Metrics;
using Cassandra.Metrics.Abstractions;

namespace Universalis.DbAccess.Metrics;

public class PrometheusDataStaxMetricsProvider : IDriverMetricsProvider
{
    public IDriverTimer Timer(string bucket, IMetric metric)
    {
        return new PrometheusDataStaxTimer(SanitizeName(metric.Name), bucket);
    }

    public IDriverMeter Meter(string bucket, IMetric metric)
    {
        return new PrometheusDataStaxMeter(SanitizeName(metric.Name), bucket);
    }

    public IDriverCounter Counter(string bucket, IMetric metric)
    {
        return new PrometheusDataStaxCounter(SanitizeName(metric.Name), bucket);
    }

    public IDriverGauge Gauge(string bucket, IMetric metric, Func<double?> valueProvider)
    {
        return new PrometheusDataStaxGauge();
    }

    public void ShutdownMetricsBucket(string bucket)
    {
    }

    private static string SanitizeName(string name)
    {
        return name.Replace('-', '_').Replace('.', '_');
    }
}