using System;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionStateTransition
{
    public required string ExecutionId { get; init; }

    public required AzureManifestExecutionStatus FromStatus { get; init; }

    public required AzureManifestExecutionStatus ToStatus { get; init; }

    public DateTimeOffset TransitionedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? Reason { get; init; }
}
