namespace MigrationBase.Core.Cloud.Azure.Governance;

/// <summary>
/// Result of evaluating whether an operator can perform a protected production action.
/// </summary>
public sealed record AzureOperatorAuthorizationEvaluation
{
    public required string OperatorId { get; init; }
    public required string EnvironmentName { get; init; }
    public required string ProtectedAction { get; init; }
    public bool IsAuthorized { get; init; }
    public bool OverrideRequired { get; init; }
    public IReadOnlyList<string> FailedRequirementIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
