namespace Migration.Workers.QueueExecutor.Options;

public sealed class SqlOperationalQueueExecutorOptions
{
    public const string SectionName = "SqlOperationalQueueExecutor";

    public bool Enabled { get; init; }

    public string WorkerId { get; init; } = $"sql-operational-worker-{Environment.MachineName}";

    public Guid? RunId { get; init; }

    public bool AutoStartRun { get; init; }

    public int StartRunBatchSize { get; init; } = 500;

    public string WorkItemType { get; init; } = "AssetMigration";

    public string? PartitionKey { get; init; }

    public int Priority { get; init; } = 0;

    public string? PayloadTemplateJson { get; init; }

    public int BatchSize { get; init; } = 25;

    public int LeaseSeconds { get; init; } = 300;

    public int PollDelaySeconds { get; init; } = 5;

    public bool RunUntilIdleAndStop { get; init; }

    public bool CompleteNoOpWorkItems { get; init; }

    public int RetryDelaySeconds { get; init; } = 300;
}
