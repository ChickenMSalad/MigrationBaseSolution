namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionPreflightRequest
{
    public required AzureConnectorExecutionRequest ExecutionRequest { get; init; }

    public AzureConnectorExecutionValidationOptions ValidationOptions { get; init; } = new();
}
