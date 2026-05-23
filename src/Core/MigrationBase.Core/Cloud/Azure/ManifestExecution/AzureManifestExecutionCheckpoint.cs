using System;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCheckpoint
{
    public required string CheckpointId { get; init; }

    public required string StepId { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? Cursor { get; init; }

    public long? ProcessedCount { get; init; }

    public string? Notes { get; init; }
}
