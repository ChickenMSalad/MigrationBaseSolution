namespace Migration.Admin.Api.Operational.Events;

public sealed record OperationalEventRecord(
    Guid OperationalEventId,
    string EventType,
    string Severity,
    string Category,
    string Source,
    string Message,
    string? PayloadJson,
    DateTimeOffset CreatedUtc);
