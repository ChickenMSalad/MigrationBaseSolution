namespace Migration.Workers.ServiceBusExecutor.Options;

internal sealed class SqlServiceBusExecutorOptions
{
    public const string SectionName = "SqlServiceBusExecutor";

    public string ServiceBusConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "migration-work-items";

    public string WorkerId { get; set; } = Environment.MachineName;

    public int MaxConcurrentCalls { get; set; } = 4;

    public int RetryDelaySeconds { get; set; } = 60;

    public bool CompleteWithoutExecutingMigration { get; set; } = false;
}
