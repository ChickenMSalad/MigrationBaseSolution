namespace MigrationBase.Core.Cloud.Azure.Operationalization;

/// <summary>
/// Represents one closure gate for the P5.1 Azure runtime topology baseline.
/// </summary>
public sealed class AzureRuntimeTopologyClosureGate
{
    public string GateId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public AzureRuntimeTopologyReadinessStatus Status { get; set; } = AzureRuntimeTopologyReadinessStatus.Unknown;

    public bool RequiredForP52 { get; set; }

    public IReadOnlyList<string> EvidenceKeys { get; set; } = Array.Empty<string>();
}
