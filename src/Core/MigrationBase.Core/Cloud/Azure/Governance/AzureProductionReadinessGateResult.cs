namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed record AzureProductionReadinessGateResult
{
    public required string GateId { get; init; }

    public required AzureProductionReadinessGateStatus Status { get; init; }

    public string? Message { get; init; }

    public string? EvidenceReference { get; init; }
}
