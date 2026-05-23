namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionValidationIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public AzureConnectorExecutionValidationSeverity Severity { get; init; } =
        AzureConnectorExecutionValidationSeverity.Error;

    public string? Field { get; init; }
}
