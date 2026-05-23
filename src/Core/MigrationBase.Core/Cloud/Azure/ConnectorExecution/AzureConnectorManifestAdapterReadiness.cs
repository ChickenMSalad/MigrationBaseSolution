namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorManifestAdapterReadiness
{
    public bool HasConnectorExecutor { get; init; }

    public bool HasManifestRequestMapper { get; init; }

    public bool HasResultMapper { get; init; }

    public bool IsReady =>
        HasConnectorExecutor &&
        HasManifestRequestMapper &&
        HasResultMapper;
}
