using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureRuntimeReadinessReport
{
    public AzureFailureRuntimeReadinessStatus Status { get; init; } =
        AzureFailureRuntimeReadinessStatus.Ready;

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureFailureRuntimeReadinessIssue> Issues { get; init; } =
        new List<AzureFailureRuntimeReadinessIssue>();

    public bool IsReady => Status == AzureFailureRuntimeReadinessStatus.Ready;
}
