namespace MigrationBase.Core.Cloud.Azure.Workers.Leases;

public sealed record AzureWorkerExecutionLeaseDescriptor(
    string LeaseName,
    string WorkerRole,
    string StoreName,
    TimeSpan LeaseDuration,
    TimeSpan RenewalInterval,
    TimeSpan AbandonmentTimeout,
    bool RequiresFencingToken,
    bool AllowsCooperativeRelease);
