using System;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchCoordinatorOptions
{
    public const string SectionName = "AzureRuntime:WorkerDispatchCoordinator";

    public bool Enabled { get; set; } = true;

    public int MaxClaimBatchSize { get; set; } = 4;

    public TimeSpan ClaimLeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    public bool StopAfterFirstClaimFailure { get; set; }
}
