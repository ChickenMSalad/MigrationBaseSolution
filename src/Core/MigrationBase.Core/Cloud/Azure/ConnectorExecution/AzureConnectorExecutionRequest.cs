using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionRequest
{
    public required string ExecutionId { get; init; }

    public required string RunId { get; init; }

    public required string ManifestId { get; init; }

    public required string ItemId { get; init; }

    public string? SourceSystem { get; init; }

    public string? TargetSystem { get; init; }

    public string? SourceIdentifier { get; init; }

    public string? TargetIdentifier { get; init; }

    public AzureConnectorExecutionMode Mode { get; init; } =
        AzureConnectorExecutionMode.ValidateOnly;

    public AzureConnectorExecutionDirection Direction { get; init; } =
        AzureConnectorExecutionDirection.SourceRead;

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
