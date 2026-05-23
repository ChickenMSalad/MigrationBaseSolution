using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Governance.Readiness;

/// <summary>
/// Captures an explicit readiness decision for a target environment without coupling the contract to a specific persistence implementation.
/// </summary>
public sealed record AzureProductionReadinessDecision
{
    public string DecisionId { get; init; } = string.Empty;

    public string ChecklistId { get; init; } = string.Empty;

    public AzureProductionReadinessDecisionStatus Status { get; init; } = AzureProductionReadinessDecisionStatus.Pending;

    public string DecidedBy { get; init; } = string.Empty;

    public DateTimeOffset? DecidedUtc { get; init; }

    public string Notes { get; init; } = string.Empty;

    public IReadOnlyList<string> BlockingItemKeys { get; init; } = Array.Empty<string>();
}
