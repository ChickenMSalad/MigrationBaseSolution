namespace Migration.Application.Operational.Telemetry;

public static class OperationalExecutionTelemetryFields
{
    public const string RunId = nameof(RunId);
    public const string WorkItemId = nameof(WorkItemId);
    public const string ManifestRowId = nameof(ManifestRowId);
    public const string WorkerId = nameof(WorkerId);
    public const string WorkItemType = nameof(WorkItemType);
    public const string PartitionKey = nameof(PartitionKey);
    public const string AttemptCount = nameof(AttemptCount);
    public const string ServiceBusMessageId = nameof(ServiceBusMessageId);
    public const string ServiceBusCorrelationId = nameof(ServiceBusCorrelationId);
    public const string ExecutionAttemptId = nameof(ExecutionAttemptId);
    public const string ExecutionDurationMs = nameof(ExecutionDurationMs);
    public const string ExecutionOutcome = nameof(ExecutionOutcome);
    public const string ErrorCode = nameof(ErrorCode);
}
