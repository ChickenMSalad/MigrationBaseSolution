namespace Migration.Admin.Api.Operational.Events;

public sealed record OperationalEventQueryResponse(
    int Skip,
    int Take,
    int Returned,
    IReadOnlyList<OperationalEventRecord> Events);


