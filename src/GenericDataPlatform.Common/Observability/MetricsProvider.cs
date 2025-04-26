using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Common.Observability
{
    /// <summary>
    /// Provider for custom metrics
    /// </summary>
    public class MetricsProvider : IMetricsProvider
    {
        private readonly Meter _meter;
        private readonly ILogger<MetricsProvider> _logger;
        private readonly Dictionary<string, Counter<long>> _counters = new Dictionary<string, Counter<long>>();
        private readonly Dictionary<string, Histogram<double>> _histograms = new Dictionary<string, Histogram<double>>();
        private readonly Dictionary<string, ObservableGauge<long>> _gauges = new Dictionary<string, ObservableGauge<long>>();

        public MetricsProvider(string serviceName, ILogger<MetricsProvider> logger)
        {
            _meter = new Meter(serviceName);
            _logger = logger;
        }

        /// <summary>
        /// Increments a counter
        /// </summary>
        public void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object>[] tags)
        {
            try
            {
                if (!_counters.TryGetValue(name, out var counter))
                {
                    counter = _meter.CreateCounter<long>(name);
                    _counters[name] = counter;
                }
                
                counter.Add(value, tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing counter {CounterName}", name);
            }
        }

        /// <summary>
        /// Records a histogram value
        /// </summary>
        public void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags)
        {
            try
            {
                if (!_histograms.TryGetValue(name, out var histogram))
                {
                    histogram = _meter.CreateHistogram<double>(name);
                    _histograms[name] = histogram;
                }
                
                histogram.Record(value, tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording histogram {HistogramName}", name);
            }
        }

        /// <summary>
        /// Creates an observable gauge
        /// </summary>
        public void CreateGauge(string name, Func<long> valueProvider)
        {
            try
            {
                if (!_gauges.TryGetValue(name, out _))
                {
                    var gauge = _meter.CreateObservableGauge(name, () => valueProvider());
                    _gauges[name] = gauge;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gauge {GaugeName}", name);
            }
        }

        /// <summary>
        /// Disposes the meter
        /// </summary>
        public void Dispose()
        {
            _meter?.Dispose();
        }
    }

    /// <summary>
    /// Interface for metrics provider
    /// </summary>
    public interface IMetricsProvider : IDisposable
    {
        void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object>[] tags);
        void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags);
        void CreateGauge(string name, Func<long> valueProvider);
    }
}
