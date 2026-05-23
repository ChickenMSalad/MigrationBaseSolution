using System;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionEvidenceRequest
{
    public required AzureConnectorExecutionRequest Request { get; init; }

    public required AzureConnectorExecutionResult Result { get; init; }

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
