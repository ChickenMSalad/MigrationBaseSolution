using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionReadinessReport
{
    public AzureConnectorExecutionReadinessStatus Status { get; init; } =
        AzureConnectorExecutionReadinessStatus.Ready;

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureConnectorExecutionReadinessIssue> Issues { get; init; } =
        new List<AzureConnectorExecutionReadinessIssue>();

    public bool IsReady => Status == AzureConnectorExecutionReadinessStatus.Ready;
}
