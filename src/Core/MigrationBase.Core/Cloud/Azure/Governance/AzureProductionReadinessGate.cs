namespace MigrationBase.Core.Cloud.Azure.Governance;

/// <summary>
/// Describes a production readiness gate that must be satisfied before an Azure-hosted migration runtime
/// is promoted, enabled, or used for a real migration execution.
/// </summary>
public sealed record AzureProductionReadinessGate
{
    public required string GateId { get; init; }

    public required string Name { get; init; }

    public required AzureProductionReadinessGateCategory Category { get; init; }

    public required AzureProductionReadinessGateSeverity Severity { get; init; }

    public string? Description { get; init; }

    public string? EvidenceKey { get; init; }

    public bool IsRequiredForProduction { get; init; } = true;
}
