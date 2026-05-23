namespace MigrationBase.Core.Cloud.Azure.Governance;

/// <summary>
/// Represents the result of evaluating production readiness gates for a target environment.
/// </summary>
public sealed record AzureProductionReadinessAssessment
{
    public required string EnvironmentName { get; init; }

    public required string DeploymentRing { get; init; }

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyCollection<AzureProductionReadinessGateResult> GateResults { get; init; } = Array.Empty<AzureProductionReadinessGateResult>();

    public bool IsProductionReady => GateResults.All(result => result.Status == AzureProductionReadinessGateStatus.Passed || result.Status == AzureProductionReadinessGateStatus.NotApplicable);
}
