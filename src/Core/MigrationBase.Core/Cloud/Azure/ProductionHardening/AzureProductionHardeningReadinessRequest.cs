namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionHardeningReadinessRequest
{
    public required string RuntimeName { get; init; }

    public bool RequireReleaseGateEvaluator { get; init; } = true;

    public bool RequireRollbackEvaluator { get; init; } = true;

    public bool RequireAbortController { get; init; } = true;

    public bool RequireReadinessChecklistBuilder { get; init; } = true;

    public bool RequireDeploymentDecisionEvaluator { get; init; } = true;
}
