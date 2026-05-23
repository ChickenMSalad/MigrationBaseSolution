using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Central SDK-free registry for operational metric descriptors.
/// </summary>
public sealed class AzureMetricRegistry : IAzureMetricRegistry
{
    private readonly IReadOnlyDictionary<string, AzureMetricDescriptor> _metrics;

    public AzureMetricRegistry(IEnumerable<AzureMetricDescriptor> metrics)
    {
        if (metrics is null) throw new ArgumentNullException(nameof(metrics));

        _metrics = metrics
            .GroupBy(metric => metric.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<AzureMetricDescriptor> Metrics => _metrics.Values.ToArray();

    public bool TryGetMetric(string name, out AzureMetricDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            descriptor = null;
            return false;
        }

        return _metrics.TryGetValue(name.Trim(), out descriptor);
    }

    public static AzureMetricRegistry CreateDefault()
    {
        return new AzureMetricRegistry(new[]
        {
            new AzureMetricDescriptor(
                "migration.worker.heartbeat.age.seconds",
                "Worker",
                "Seconds",
                AzureMetricKind.Gauge,
                new[] { "environment", "hostRole", "workerId" },
                "Age of the most recent worker heartbeat."),
            new AzureMetricDescriptor(
                "migration.workitem.started.count",
                "Execution",
                "Count",
                AzureMetricKind.Counter,
                new[] { "environment", "hostRole", "connector", "migrationType" },
                "Number of work items started by workers."),
            new AzureMetricDescriptor(
                "migration.workitem.completed.count",
                "Execution",
                "Count",
                AzureMetricKind.Counter,
                new[] { "environment", "hostRole", "connector", "migrationType", "outcome" },
                "Number of work items completed by outcome."),
            new AzureMetricDescriptor(
                "migration.workitem.duration.ms",
                "Execution",
                "Milliseconds",
                AzureMetricKind.Duration,
                new[] { "environment", "hostRole", "connector", "migrationType", "outcome" },
                "Elapsed processing time for a work item."),
            new AzureMetricDescriptor(
                "migration.queue.depth.count",
                "Queue",
                "Count",
                AzureMetricKind.Gauge,
                new[] { "environment", "queueName", "priority" },
                "Observed queue depth for migration work."),
            new AzureMetricDescriptor(
                "migration.poisonwork.count",
                "Reliability",
                "Count",
                AzureMetricKind.Counter,
                new[] { "environment", "queueName", "reason" },
                "Number of work items moved to poison handling."),
            new AzureMetricDescriptor(
                "migration.lease.renewal.failed.count",
                "Reliability",
                "Count",
                AzureMetricKind.Counter,
                new[] { "environment", "hostRole", "reason" },
                "Number of failed execution lease renewals."),
            new AzureMetricDescriptor(
                "migration.circuitbreaker.open.count",
                "Reliability",
                "Count",
                AzureMetricKind.Counter,
                new[] { "environment", "hostRole", "breakerName" },
                "Number of times a runtime circuit breaker opened.")
        });
    }
}
