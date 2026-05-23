namespace MigrationBase.Core.Cloud.Azure.Workers.Leases;

public sealed record AzureWorkerExecutionLeasePolicy(
    TimeSpan MinimumLeaseDuration,
    TimeSpan MaximumLeaseDuration,
    TimeSpan DefaultRenewalInterval,
    int MaximumRenewalFailures,
    bool RequireMonotonicFencingTokens,
    bool ReleaseLeaseOnGracefulShutdown);
