using System;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCheckpointRequest
{
    public required string ExecutionId { get; init; }

    public required string StepId { get; init; }

    public string? Cursor { get; init; }

    public long? ProcessedCount { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
