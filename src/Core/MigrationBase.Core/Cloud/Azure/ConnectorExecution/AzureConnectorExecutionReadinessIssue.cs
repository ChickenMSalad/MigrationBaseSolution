namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionReadinessIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Component { get; init; }
}
