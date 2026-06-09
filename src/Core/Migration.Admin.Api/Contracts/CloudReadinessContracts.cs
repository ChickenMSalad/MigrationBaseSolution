namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Aggregate cloud readiness response for deployment diagnostics.
/// This intentionally contains no secrets.
/// </summary>
public sealed record CloudReadinessSummaryDescriptor(
    string EnvironmentName,
    bool IsDevelopment,
    bool IsCloudReady,
    int WarningCount,
    IReadOnlyList<CloudReadinessCheckDescriptor> Checks,
    IReadOnlyList<string> Warnings);

public sealed record CloudReadinessCheckDescriptor(
    string Name,
    string Status,
    IReadOnlyList<string> Warnings);

public static class CloudReadinessStatuses
{
    public const string Ready = "ready";
    public const string Warning = "warning";
    public const string NotReady = "notReady";
}


