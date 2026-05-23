using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Captures the observability surface that is expected to be handed to operators for a runtime environment.
/// </summary>
public sealed class AzureObservabilityHandoffDescriptor
{
    public AzureObservabilityHandoffDescriptor(
        string environmentName,
        IEnumerable<string>? dashboardKeys = null,
        IEnumerable<string>? alertRuleKeys = null,
        IEnumerable<string>? metricKeys = null,
        IEnumerable<string>? healthSignalKeys = null)
    {
        EnvironmentName = string.IsNullOrWhiteSpace(environmentName) ? throw new ArgumentException("Environment name is required.", nameof(environmentName)) : environmentName.Trim();
        DashboardKeys = Normalize(dashboardKeys);
        AlertRuleKeys = Normalize(alertRuleKeys);
        MetricKeys = Normalize(metricKeys);
        HealthSignalKeys = Normalize(healthSignalKeys);
    }

    public string EnvironmentName { get; }

    public IReadOnlyList<string> DashboardKeys { get; }

    public IReadOnlyList<string> AlertRuleKeys { get; }

    public IReadOnlyList<string> MetricKeys { get; }

    public IReadOnlyList<string> HealthSignalKeys { get; }

    private static IReadOnlyList<string> Normalize(IEnumerable<string>? values)
    {
        return new ReadOnlyCollection<string>((values ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }
}
