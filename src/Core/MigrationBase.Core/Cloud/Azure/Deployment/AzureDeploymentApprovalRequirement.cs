namespace MigrationBase.Core.Cloud.Azure.Deployment;

public sealed record AzureDeploymentApprovalRequirement
{
    public required string RequirementId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? AppliesToEnvironment { get; init; }
    public string? AppliesToRing { get; init; }
    public bool IsRequired { get; init; } = true;
    public int MinimumApproverCount { get; init; } = 1;
    public IReadOnlyList<string> ApproverRoles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredEvidenceKeys { get; init; } = Array.Empty<string>();
}
