using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Governance.Readiness;

/// <summary>
/// A single production-readiness control that can be satisfied by evidence collected from deployment, telemetry, SQL state, or operator review.
/// </summary>
public sealed record AzureProductionReadinessChecklistItem
{
    public string Key { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public AzureProductionReadinessDomain Domain { get; init; }

    public AzureProductionReadinessSeverity Severity { get; init; } = AzureProductionReadinessSeverity.Required;

    public string EvidenceHint { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
