namespace Migration.Workers.QueueExecutor.Options;

public sealed class OperationalQueuePublisherOptions
{
    public const string SectionName = "OperationalQueuePublisher";

    public string? ConnectionString { get; init; }

    public string QueueName { get; init; } = "migration-operational-work-items";
}
