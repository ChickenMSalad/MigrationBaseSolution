namespace Migration.Application.Operational.Telemetry;

public static class OperationalExecutionActivityTags
{
    public const string RunId = "migration.run.id";
    public const string WorkItemId = "migration.work_item.id";
    public const string ManifestRowId = "migration.manifest_row.id";
    public const string WorkItemType = "migration.work_item.type";
    public const string AttemptCount = "migration.work_item.attempt_count";
    public const string PartitionKey = "migration.partition_key";
    public const string WorkerId = "migration.worker.id";
    public const string ServiceBusCorrelationId = "messaging.servicebus.correlation_id";
    public const string ServiceBusMessageId = "messaging.message.id";
    public const string ServiceBusDeliveryCount = "messaging.servicebus.delivery_count";
    public const string ExecutionDurationMs = "migration.execution.duration_ms";
    public const string ExecutionSucceeded = "migration.execution.succeeded";
    public const string ErrorCode = "migration.error.code";
    public const string Retryable = "migration.error.retryable";
}
