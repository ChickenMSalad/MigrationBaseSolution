namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed record ProductionGovernanceCloseoutResult
{
    public required ProductionGovernanceCloseoutStatus Status { get; init; }

    public IReadOnlyList<string> BlockingItems { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public bool CanHandoff => Status == ProductionGovernanceCloseoutStatus.ReadyForOperationalHandoff || Status == ProductionGovernanceCloseoutStatus.Accepted;
}
