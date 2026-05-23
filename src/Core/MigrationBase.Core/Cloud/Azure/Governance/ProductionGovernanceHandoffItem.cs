namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed record ProductionGovernanceHandoffItem
{
    public required string Key { get; init; }

    public required ProductionGovernanceHandoffArea Area { get; init; }

    public required string Description { get; init; }

    public ProductionGovernanceCloseoutStatus Status { get; init; } = ProductionGovernanceCloseoutStatus.NotStarted;

    public string? EvidenceReference { get; init; }

    public string? Owner { get; init; }

    public string? Notes { get; init; }
}
