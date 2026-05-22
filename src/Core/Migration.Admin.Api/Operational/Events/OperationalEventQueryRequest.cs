namespace Migration.Admin.Api.Operational.Events;

public sealed record OperationalEventQueryRequest(
    string? Severity,
    string? Category,
    string? EventType,
    int Skip,
    int Take);
