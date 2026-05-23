using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionResult
{
    public required string ItemId { get; init; }

    public AzureConnectorExecutionStatus Status { get; init; } =
        AzureConnectorExecutionStatus.Succeeded;

    public string? SourceIdentifier { get; init; }

    public string? TargetIdentifier { get; init; }

    public string? Message { get; init; }

    public string? ErrorCode { get; init; }

    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Evidence { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
