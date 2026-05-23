namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed record AzureOperationalFreezeDescriptor
{
    public string EnvironmentName { get; init; } = string.Empty;

    public AzureOperationalFreezeScope Scope { get; init; } = AzureOperationalFreezeScope.Unknown;

    public string ScopeKey { get; init; } = string.Empty;

    public string FreezeCode { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public string RequestedBy { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ExpiresAtUtc { get; init; }

    public bool BlocksPromotion { get; init; } = true;

    public bool BlocksExecution { get; init; } = true;

    public bool BlocksReplay { get; init; } = true;

    public bool BlocksDeployment { get; init; }

    public IReadOnlyCollection<string> AllowedBypassRoles { get; init; } = Array.Empty<string>();
}
