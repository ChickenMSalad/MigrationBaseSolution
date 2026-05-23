using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Observability.Dashboards;

/// <summary>
/// Describes an operator-facing Azure observability dashboard without binding the runtime to a specific Azure SDK.
/// </summary>
public sealed class AzureDashboardDescriptor
{
    public string DashboardId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public string WorkloadName { get; init; } = string.Empty;

    public string DashboardType { get; init; } = string.Empty;

    public IReadOnlyList<AzureDashboardPanelDescriptor> Panels { get; init; } = Array.Empty<AzureDashboardPanelDescriptor>();

    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
