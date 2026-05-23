namespace MigrationBase.Core.Cloud.Azure.Validation.Throughput;

public sealed class AzureMigrationThroughputValidationProfile
{
    public string ProfileName { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string WorkloadName { get; init; } = string.Empty;
    public int ExpectedManifestRows { get; init; }
    public int MaxConcurrentWorkers { get; init; }
    public int TargetItemsPerMinute { get; init; }
    public int MinimumSustainedMinutes { get; init; }
    public TimeSpan MaximumAcceptableLatency { get; init; } = TimeSpan.FromMinutes(5);
    public bool RequireSqlRunStateVerification { get; init; } = true;
    public bool RequireOperationalEventCorrelation { get; init; } = true;
}
