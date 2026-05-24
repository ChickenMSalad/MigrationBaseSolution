using System;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionDeploymentDecisionRequest
{
    public required string ReleaseId { get; init; }

    public required AzureProductionReleaseGateResult ReleaseGateResult { get; init; }

    public AzureProductionReadinessChecklist? ReadinessChecklist { get; init; }

    public AzureProductionRollbackDecision? RollbackDecision { get; init; }

    public bool RequireReadinessChecklist { get; init; } = true;

    public bool RequireRollbackDecision { get; init; } = true;

    public bool OperatorOverrideGranted { get; init; }

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
