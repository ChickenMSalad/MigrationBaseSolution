namespace MigrationBase.Core.Cloud.Azure.Governance;

/// <summary>
/// Captures a requested operator override for an otherwise blocked production operation.
/// </summary>
public sealed record AzureOperatorOverrideRequest
{
    public required string RequestId { get; init; }
    public required string RequestedBy { get; init; }
    public required string EnvironmentName { get; init; }
    public required string ProtectedAction { get; init; }
    public required string Justification { get; init; }
    public DateTimeOffset RequestedAtUtc { get; init; }
    public TimeSpan RequestedDuration { get; init; }
    public IReadOnlyDictionary<string, string> Evidence { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
