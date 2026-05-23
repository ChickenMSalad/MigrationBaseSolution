namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public interface IAzureConnectorExecutionValidator
{
    AzureConnectorExecutionValidationResult Validate(
        AzureConnectorExecutionRequest request,
        AzureConnectorExecutionValidationOptions? options = null);
}
