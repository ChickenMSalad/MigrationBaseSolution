using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Governance.Readiness;

/// <summary>
/// Describes the production-readiness evidence expected before allowing a real migration execution environment to be promoted.
/// </summary>
public sealed record AzureProductionReadinessChecklist
{
    public string ChecklistId { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public string DeploymentRing { get; init; } = string.Empty;

    public IReadOnlyList<AzureProductionReadinessChecklistItem> Items { get; init; } = Array.Empty<AzureProductionReadinessChecklistItem>();

    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
