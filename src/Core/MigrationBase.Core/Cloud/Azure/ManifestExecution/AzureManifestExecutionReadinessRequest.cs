namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionReadinessRequest
{
    public required string RunId { get; init; }

    public required string ManifestId { get; init; }

    public bool RequirePlanBuilder { get; init; } = true;

    public bool RequireContextFactory { get; init; } = true;

    public bool RequireBatchProvider { get; init; } = true;

    public bool RequireBatchRunner { get; init; } = true;

    public bool RequireCheckpointStore { get; init; } = true;

    public bool RequireCompletionSink { get; init; } = true;
}
