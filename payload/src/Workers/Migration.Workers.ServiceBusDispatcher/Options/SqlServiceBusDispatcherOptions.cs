namespace Migration.Workers.ServiceBusDispatcher.Options;

public sealed class SqlServiceBusDispatcherOptions
{
    public string SqlConnectionString { get; set; } = string.Empty;

    public string ServiceBusConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "migration-work-items";

    public string WorkerId { get; set; } = Environment.MachineName;

    public int BatchSize { get; set; } = 25;

    public int PollIntervalSeconds { get; set; } = 15;

    public int LeaseSeconds { get; set; } = 300;

    public bool Enabled { get; set; }
}
