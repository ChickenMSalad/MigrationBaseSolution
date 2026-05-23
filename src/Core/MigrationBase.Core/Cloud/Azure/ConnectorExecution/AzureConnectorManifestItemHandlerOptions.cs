namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorManifestItemHandlerOptions
{
    public const string SectionName = "AzureRuntime:ConnectorManifestItemHandler";

    public bool Enabled { get; set; } = true;

    public AzureConnectorExecutionDirection DefaultDirection { get; set; } =
        AzureConnectorExecutionDirection.SourceRead;

    public bool FailOnMissingSourceIdentifier { get; set; } = true;

    public bool UseNoOpConnectorExecutor { get; set; } = true;
}
