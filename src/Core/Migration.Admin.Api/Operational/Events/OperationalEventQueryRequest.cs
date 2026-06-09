namespace Migration.Admin.Api.Operational.Events;

public sealed record OperationalEventQueryRequest(
    string? Severity,
    string? Category,
    string? EventType,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    Guid? ExecutionSessionId,
    Guid? MigrationRunId,
    int Skip,
    int Take);


