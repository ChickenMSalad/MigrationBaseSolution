namespace MigrationBase.Core.Cloud.Azure.Topology;

/// <summary>
/// Represents one topology, configuration, or operational-readiness item that must be
/// understood before worker stabilization begins.
/// </summary>
public sealed class AzureRuntimeTopologyHandoffItem
{
    public string Key { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool RequiredForP52 { get; init; }

    public AzureRuntimeTopologyHandoffStatus Status { get; init; } = AzureRuntimeTopologyHandoffStatus.Unknown;

    public string? Notes { get; init; }
}
