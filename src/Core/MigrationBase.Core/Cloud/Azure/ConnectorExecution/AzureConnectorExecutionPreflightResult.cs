namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionPreflightResult
{
    public required AzureConnectorExecutionValidationResult Validation { get; init; }

    public bool CanExecute => Validation.IsValid;
}
