namespace Migration.Orchestration.Options;

public sealed class MigrationExecutionOptions
{
    public const string SectionName = "MigrationExecution";

    /// <summary>
    /// JsonFile keeps the existing local behavior. AzureTable enables the durable Step 6 store.
    /// </summary>
    public string StateStore { get; set; } = "JsonFile";

    /// <summary>
    /// Optional configured progress sinks. Logging is always registered. GenericMigration.Console can still
    /// call AddConsoleMigrationProgress() explicitly, so existing console output remains unchanged.
    /// Supported values: Console, AzureQueue.
    /// </summary>
    public List<string> ProgressSinks { get; set; } = new();

    public bool ResumeCompletedWorkItems { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = 1;
    public bool StopOnFirstError { get; set; }
    public bool ContinueOnValidationError { get; set; } = true;
    public string StatePath { get; set; } = "Runtime/migration-state";
    public AzureTableMigrationExecutionStateStoreOptions AzureTableState { get; set; } = new();
    public AzureQueueMigrationProgressOptions AzureQueueProgress { get; set; } = new();
    public RetryOptions Retry { get; set; } = new();
    public ValidationOptions Validation { get; set; } = new();
}

public sealed class AzureTableMigrationExecutionStateStoreOptions
{
    public const string SectionName = "MigrationExecution:AzureTableState";

    public string? ConnectionString { get; set; }
    public string TableName { get; set; } = "MigrationExecutionState";
    public bool CreateTableIfNotExists { get; set; } = true;
}

public sealed class AzureQueueMigrationProgressOptions
{
    public const string SectionName = "MigrationExecution:AzureQueueProgress";

    /// <summary>
    /// Storage account connection string. For local dev, UseDevelopmentStorage=true is fine.
    /// In cloud, prefer managed identity later; this drop-in intentionally keeps the first version simple.
    /// </summary>
    public string? ConnectionString { get; set; }

    public string QueueName { get; set; } = "migration-progress";
    public bool CreateQueueIfMissing { get; set; } = true;

    /// <summary>
    /// Azure Queue messages have a practical 64 KiB limit. If an event serializes larger than this,
    /// the sink will trim the free-form Properties payload before sending.
    /// </summary>
    public int MaxMessageBytes { get; set; } = 60 * 1024;
}

public sealed class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int InitialDelayMilliseconds { get; set; } = 500;
    public int MaxDelayMilliseconds { get; set; } = 30_000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool UseJitter { get; set; } = true;
    public List<string> RetryableExceptionTypeNames { get; set; } = new();
    public List<string> NonRetryableExceptionTypeNames { get; set; } = new()
    {
        "InvalidOperationException",
        "ArgumentException",
        "FileNotFoundException"
    };
}

public sealed class ValidationOptions
{
    /// <summary>
    /// Fail a row before target write when the mapped payload has no binary location.
    /// Defaults to true because most DAM target connectors require a binary.
    /// </summary>
    public bool RequireBinaryForTargetWrites { get; set; } = true;

    /// <summary>
    /// Optional manifest columns that must be present and non-empty for every row.
    /// Example: ["webdam_id"] for direct WebDam migrations.
    /// </summary>
    public List<string> RequiredManifestColumns { get; set; } = new();

    /// <summary>
    /// If true, warnings returned by validation steps become row-blocking errors.
    /// </summary>
    public bool TreatWarningsAsErrors { get; set; }
}
