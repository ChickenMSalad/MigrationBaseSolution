namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionOptions
{
    public const string SectionName = "AzureRuntime:ConnectorExecution";

    public bool Enabled { get; set; } = true;

    public AzureConnectorExecutionMode DefaultMode { get; set; } =
        AzureConnectorExecutionMode.ValidateOnly;

    public bool RequireSourceIdentifier { get; set; } = true;

    public bool RequireTargetIdentifierForWrite { get; set; }

    public bool UseNoOpConnectorExecutor { get; set; } = true;
}
