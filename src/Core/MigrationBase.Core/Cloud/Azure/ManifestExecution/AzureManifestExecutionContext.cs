using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionContext
{
    public required string ExecutionId { get; init; }

    public required AzureManifestExecutionPlan Plan { get; init; }

    public AzureManifestExecutionStatus Status { get; init; } = AzureManifestExecutionStatus.NotStarted;

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public IReadOnlyList<AzureManifestExecutionCheckpoint> Checkpoints { get; init; } =
        new List<AzureManifestExecutionCheckpoint>();

    public IReadOnlyDictionary<string, string> RuntimeProperties { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
