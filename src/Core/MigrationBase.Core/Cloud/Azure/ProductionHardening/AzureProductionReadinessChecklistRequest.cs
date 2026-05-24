namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReadinessChecklistRequest
{
    public required string ReleaseId { get; init; }

    public AzureProductionReleaseGateResult? ReleaseGateResult { get; init; }

    public AzureProductionRollbackDecision? RollbackDecision { get; init; }

    public bool IncludeReleaseGateCheck { get; init; } = true;

    public bool IncludeRollbackCheck { get; init; } = true;

    public bool IncludeOperatorSignoffCheck { get; init; } = true;

    public bool OperatorSignoffGranted { get; init; }
}
