namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureRetryRuntimeOptions
{
    public const string SectionName = "AzureRuntime:RetryRuntime";

    public bool Enabled { get; set; } = true;

    public int MaxAttempts { get; set; } = 3;

    public string InitialDelay { get; set; } = "00:00:05";

    public string MaxDelay { get; set; } = "00:05:00";

    public double BackoffMultiplier { get; set; } = 2.0;

    public bool RetryUnknownFailures { get; set; }
}
