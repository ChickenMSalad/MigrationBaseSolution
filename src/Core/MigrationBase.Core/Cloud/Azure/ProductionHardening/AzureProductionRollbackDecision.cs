using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionRollbackDecision
{
    public required string ReleaseId { get; init; }

    public AzureProductionRollbackDecisionStatus Status { get; init; } =
        AzureProductionRollbackDecisionStatus.NotEvaluated;

    public AzureProductionRollbackTrigger Trigger { get; init; } =
        AzureProductionRollbackTrigger.Unknown;

    public string? Reason { get; init; }

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Evidence { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool ShouldRollback =>
        Status is AzureProductionRollbackDecisionStatus.RollbackRecommended or
            AzureProductionRollbackDecisionStatus.RollbackRequired;
}
