using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionReadinessReport
{
    public AzureManifestExecutionReadinessStatus Status { get; init; } =
        AzureManifestExecutionReadinessStatus.Ready;

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureManifestExecutionReadinessIssue> Issues { get; init; } =
        new List<AzureManifestExecutionReadinessIssue>();

    public bool IsReady => Status == AzureManifestExecutionReadinessStatus.Ready;
}
