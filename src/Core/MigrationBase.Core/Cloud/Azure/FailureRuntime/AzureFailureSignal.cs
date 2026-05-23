using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureSignal
{
    public required string SignalId { get; init; }

    public required string Source { get; init; }

    public string? RunId { get; init; }

    public string? ManifestId { get; init; }

    public string? WorkItemId { get; init; }

    public string? ErrorCode { get; init; }

    public string? Message { get; init; }

    public int AttemptNumber { get; init; }

    public DateTimeOffset ObservedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
