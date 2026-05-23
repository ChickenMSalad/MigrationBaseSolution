namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed record ProductionGovernanceCloseoutManifest
{
    public required string EnvironmentName { get; init; }

    public required string DeploymentRing { get; init; }

    public required string RequestedBy { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<ProductionGovernanceHandoffItem> HandoffItems { get; init; } = Array.Empty<ProductionGovernanceHandoffItem>();

    public bool IsAccepted => HandoffItems.Count > 0 && HandoffItems.All(item => item.Status == ProductionGovernanceCloseoutStatus.Accepted);
}
