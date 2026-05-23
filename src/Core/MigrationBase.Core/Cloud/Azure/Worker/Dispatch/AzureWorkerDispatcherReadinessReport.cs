using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatcherReadinessReport
{
    public AzureWorkerDispatcherReadinessStatus Status { get; init; } =
        AzureWorkerDispatcherReadinessStatus.Ready;

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureWorkerDispatcherReadinessIssue> Issues { get; init; } =
        new List<AzureWorkerDispatcherReadinessIssue>();

    public bool IsReady => Status == AzureWorkerDispatcherReadinessStatus.Ready;
}
