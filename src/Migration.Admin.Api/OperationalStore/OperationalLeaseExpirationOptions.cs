namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalLeaseExpirationOptions
{
    public const string SectionName = "OperationalLeaseExpiration";

    public int LeaseTimeoutMinutes { get; init; } = 30;

    public int MaxReclaimCount { get; init; } = 100;
}
