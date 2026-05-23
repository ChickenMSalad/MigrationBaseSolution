namespace MigrationBase.Core.Cloud.Azure.Governance;

/// <summary>
/// Describes an operator authorization requirement that must be satisfied before a protected production action can proceed.
/// </summary>
public sealed record AzureOperatorAuthorizationRequirement
{
    public required string RequirementId { get; init; }
    public required string DisplayName { get; init; }
    public required string ProtectedAction { get; init; }
    public required string MinimumRole { get; init; }
    public bool RequiresJustification { get; init; } = true;
    public bool RequiresSecondOperator { get; init; }
    public bool RequiresDeploymentRingMatch { get; init; } = true;
    public IReadOnlyList<string> AllowedEnvironments { get; init; } = Array.Empty<string>();
}
