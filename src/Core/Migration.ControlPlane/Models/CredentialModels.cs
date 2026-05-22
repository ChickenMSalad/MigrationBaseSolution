namespace Migration.ControlPlane.Models;

public sealed record CreateCredentialSetRequest(
    string DisplayName,
    string ConnectorType,
    string ConnectorRole,
    Dictionary<string, string?> Values,
    IReadOnlyCollection<string>? SecretKeys = null);

public sealed record UpdateCredentialSetRequest(
    string? DisplayName = null,
    string? ConnectorType = null,
    string? ConnectorRole = null,
    Dictionary<string, string?>? Values = null,
    IReadOnlyCollection<string>? SecretKeys = null);

public sealed record CredentialSetRecord
{
    public required string CredentialSetId { get; init; }
    public required string DisplayName { get; init; }
    public required string ConnectorType { get; init; }
    public required string ConnectorRole { get; init; }
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;

    // Local-dev file-backed store only. Do not use this plaintext store for production secrets.
    public Dictionary<string, string?> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SecretKeys { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record CredentialSetSummary(
    string CredentialSetId,
    string DisplayName,
    string ConnectorType,
    string ConnectorRole,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyCollection<string> SecretKeys);

public sealed record CredentialTestResult(
    string CredentialSetId,
    string ConnectorType,
    string ConnectorRole,
    bool Success,
    string Message,
    DateTimeOffset TestedUtc);
