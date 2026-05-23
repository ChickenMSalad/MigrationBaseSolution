namespace MigrationBase.Core.Cloud.Azure.Topology;

/// <summary>
/// Captures the closeout view of P5.1 so the worker stabilization phase can consume
/// a single topology-readiness manifest instead of re-discovering each contract.
/// </summary>
public sealed class AzureRuntimeTopologyHandoffManifest
{
    public string EnvironmentName { get; init; } = string.Empty;

    public string DeploymentRing { get; init; } = string.Empty;

    public string RuntimeProfile { get; init; } = string.Empty;

    public string GeneratedBy { get; init; } = string.Empty;

    public DateTimeOffset? GeneratedAtUtc { get; init; }

    public IReadOnlyList<AzureRuntimeTopologyHandoffItem> Items { get; init; } = Array.Empty<AzureRuntimeTopologyHandoffItem>();
}
