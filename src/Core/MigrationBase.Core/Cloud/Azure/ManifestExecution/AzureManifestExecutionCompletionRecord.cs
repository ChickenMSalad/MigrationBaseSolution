using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCompletionRecord
{
    public required string CompletionId { get; init; }

    public required string ExecutionId { get; init; }

    public required string RunId { get; init; }

    public required string ManifestId { get; init; }

    public required AzureManifestExecutionCompletionStatus Status { get; init; }

    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? Reason { get; init; }

    public string? ErrorCode { get; init; }

    public string? FinalCursor { get; init; }

    public long? ProcessedCount { get; init; }

    public IReadOnlyDictionary<string, string> Evidence { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
