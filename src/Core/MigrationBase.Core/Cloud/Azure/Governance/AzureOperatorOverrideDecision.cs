namespace MigrationBase.Core.Cloud.Azure.Governance;

/// <summary>
/// Represents the approval decision for an operator override request.
/// </summary>
public sealed record AzureOperatorOverrideDecision
{
    public required string RequestId { get; init; }
    public required string DecidedBy { get; init; }
    public required AzureOperatorOverrideDecisionStatus Status { get; init; }
    public required string DecisionReason { get; init; }
    public DateTimeOffset DecidedAtUtc { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
