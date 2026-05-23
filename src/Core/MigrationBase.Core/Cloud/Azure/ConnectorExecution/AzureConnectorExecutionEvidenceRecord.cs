using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionEvidenceRecord
{
    public required string EvidenceId { get; init; }

    public required string ExecutionId { get; init; }

    public required string RunId { get; init; }

    public required string ManifestId { get; init; }

    public required string ItemId { get; init; }

    public AzureConnectorExecutionStatus Status { get; init; }

    public string? SourceIdentifier { get; init; }

    public string? TargetIdentifier { get; init; }

    public string? ErrorCode { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Evidence { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
