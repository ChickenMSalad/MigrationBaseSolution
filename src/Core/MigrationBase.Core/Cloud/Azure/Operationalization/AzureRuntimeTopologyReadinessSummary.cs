namespace MigrationBase.Core.Cloud.Azure.Operationalization;

/// <summary>
/// Summarizes whether the Azure runtime topology baseline is ready to feed worker stabilization.
/// </summary>
public sealed class AzureRuntimeTopologyReadinessSummary
{
    public string EnvironmentName { get; set; } = string.Empty;

    public string DeploymentRing { get; set; } = string.Empty;

    public DateTimeOffset EvaluatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public AzureRuntimeTopologyReadinessStatus OverallStatus { get; set; } = AzureRuntimeTopologyReadinessStatus.Unknown;

    public IReadOnlyList<AzureRuntimeTopologyClosureGate> Gates { get; set; } = Array.Empty<AzureRuntimeTopologyClosureGate>();

    public bool IsReadyForWorkerStabilization()
    {
        if (Gates.Count == 0)
        {
            return false;
        }

        return Gates
            .Where(gate => gate.RequiredForP52)
            .All(gate => gate.Status == AzureRuntimeTopologyReadinessStatus.Ready || gate.Status == AzureRuntimeTopologyReadinessStatus.Deferred);
    }
}
