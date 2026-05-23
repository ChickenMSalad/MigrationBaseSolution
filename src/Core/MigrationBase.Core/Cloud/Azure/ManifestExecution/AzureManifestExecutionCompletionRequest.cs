using System;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCompletionRequest
{
    public required AzureManifestExecutionContext Context { get; init; }

    public required AzureManifestExecutionCompletionStatus Status { get; init; }

    public string? Reason { get; init; }

    public string? ErrorCode { get; init; }

    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public AzureManifestExecutionCheckpoint? FinalCheckpoint { get; init; }
}
