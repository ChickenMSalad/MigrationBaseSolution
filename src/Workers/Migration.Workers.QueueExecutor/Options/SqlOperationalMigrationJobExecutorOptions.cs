namespace Migration.Workers.QueueExecutor.Options;

public sealed class SqlOperationalMigrationJobExecutorOptions
{
    public const string SectionName = "SqlOperationalMigrationJobExecutor";

    /// <summary>
    /// Enables the real IMigrationJobRunner-backed executor for SQL operational work items.
    /// Keep false until the SQL lifecycle/no-op smoke path has been validated.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// When true, the executor adds SQL operational context values into the job settings bag.
    /// </summary>
    public bool AddOperationalContextSettings { get; init; } = true;

    /// <summary>
    /// If true, a completed job summary with failed work items causes a retryable SQL work-item failure.
    /// If false, job-level failures are terminal SQL work-item failures.
    /// </summary>
    public bool TreatJobFailuresAsRetryable { get; init; }

    /// <summary>
    /// If true, unexpected executor exceptions are marked retryable at the SQL work-item level.
    /// </summary>
    public bool TreatUnhandledExceptionsAsRetryable { get; init; } = true;

    public int RetryDelaySeconds { get; init; } = 300;
}
