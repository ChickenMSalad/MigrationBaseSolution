namespace MigrationBase.Core.Cloud.Azure;

public sealed class AzureQueueTopologyOptions
{
    public string Provider { get; set; } = "Sql";

    public string WorkQueueName { get; set; } = "migration-work";

    public string ReplayQueueName { get; set; } = "migration-replay";

    public string DeadLetterQueueName { get; set; } = "migration-deadletter";

    public int MaxConcurrentWorkers { get; set; } = 4;

    public int LeaseDurationSeconds { get; set; } = 120;

    public int LeaseRenewalSeconds { get; set; } = 30;

    public int VisibilityTimeoutSeconds { get; set; } = 300;

    public int MaxDeliveryAttempts { get; set; } = 5;
}
