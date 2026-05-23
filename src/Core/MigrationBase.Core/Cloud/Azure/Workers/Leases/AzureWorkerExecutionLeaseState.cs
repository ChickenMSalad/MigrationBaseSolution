namespace MigrationBase.Core.Cloud.Azure.Workers.Leases;

public sealed record AzureWorkerExecutionLeaseState(
    string LeaseName,
    string WorkerInstanceId,
    string FencingToken,
    DateTimeOffset AcquiredAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? LastRenewedAtUtc,
    AzureWorkerExecutionLeaseStatus Status);
