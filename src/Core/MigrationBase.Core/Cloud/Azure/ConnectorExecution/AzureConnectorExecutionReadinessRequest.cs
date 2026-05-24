namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionReadinessRequest
{
    public required string RuntimeName { get; init; }

    public bool RequireConnectorExecutor { get; init; } = true;

    public bool RequireManifestItemHandler { get; init; } = true;

    public bool RequireExecutionValidator { get; init; } = true;

    public bool RequirePreflight { get; init; } = true;

    public bool RequireEvidenceSink { get; init; } = true;
}
