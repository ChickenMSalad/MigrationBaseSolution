namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public enum AzureConnectorExecutionStatus
{
    Succeeded = 0,
    Skipped = 1,
    Failed = 2,
    Deferred = 3
}
