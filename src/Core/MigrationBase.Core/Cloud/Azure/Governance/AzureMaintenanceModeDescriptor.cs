namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed record AzureMaintenanceModeDescriptor
{
    public string EnvironmentName { get; init; } = string.Empty;

    public AzureMaintenanceModeState State { get; init; } = AzureMaintenanceModeState.Unknown;

    public AzureOperationalFreezeScope Scope { get; init; } = AzureOperationalFreezeScope.Unknown;

    public string ScopeKey { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public string RequestedBy { get; init; } = string.Empty;

    public DateTimeOffset? StartsAtUtc { get; init; }

    public DateTimeOffset? ExpiresAtUtc { get; init; }

    public bool BlocksNewRuns { get; init; } = true;

    public bool BlocksNewWorkItems { get; init; } = true;

    public bool AllowsInFlightDrain { get; init; } = true;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
