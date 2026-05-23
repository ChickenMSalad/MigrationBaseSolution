namespace MigrationBase.Core.Cloud.Azure.Deployment;

public sealed record AzureDeploymentApprovalGate
{
    public required string GateId { get; init; }
    public required string DeploymentTargetId { get; init; }
    public required string EnvironmentName { get; init; }
    public required string DeploymentRing { get; init; }
    public AzureDeploymentApprovalDecision Decision { get; init; } = AzureDeploymentApprovalDecision.Pending;
    public DateTimeOffset? RequestedAtUtc { get; init; }
    public DateTimeOffset? DecidedAtUtc { get; init; }
    public string? RequestedBy { get; init; }
    public string? DecidedBy { get; init; }
    public string? DecisionReason { get; init; }
    public IReadOnlyList<AzureDeploymentApprovalRequirement> Requirements { get; init; } = Array.Empty<AzureDeploymentApprovalRequirement>();

    public bool BlocksDeployment => Decision is AzureDeploymentApprovalDecision.Pending or AzureDeploymentApprovalDecision.Rejected or AzureDeploymentApprovalDecision.Expired;
}
