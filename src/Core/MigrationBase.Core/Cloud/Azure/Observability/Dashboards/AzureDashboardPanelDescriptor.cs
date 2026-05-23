using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Observability.Dashboards;

/// <summary>
/// Describes a single dashboard panel and the telemetry signal it visualizes.
/// </summary>
public sealed class AzureDashboardPanelDescriptor
{
    public string PanelId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string SignalName { get; init; } = string.Empty;

    public string VisualizationType { get; init; } = string.Empty;

    public string QueryHint { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Dimensions { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
