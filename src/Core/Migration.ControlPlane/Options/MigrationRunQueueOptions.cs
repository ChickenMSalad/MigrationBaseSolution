namespace Migration.ControlPlane.Options;

public sealed class MigrationRunQueueOptions
{
    public const string SectionName = "MigrationRunQueue";

    /// <summary>
    /// None disables queue publishing. AzureQueue publishes JSON run messages to Azure Storage Queue.
    /// </summary>
    public string Provider { get; init; } = "None";

    public string? ConnectionString { get; init; }

    public string QueueName { get; init; } = "migration-runs";

    public bool CreateIfMissing { get; init; } = true;
}
