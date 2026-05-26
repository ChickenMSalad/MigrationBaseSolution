namespace Migration.Workers.ServiceBusExecutor.Processing;

internal sealed record ServiceBusWorkItemMessage(
    long WorkItemId,
    Guid RunId,
    long SequenceNumber,
    string? PayloadJson,
    DateTimeOffset DispatchedAtUtc,
    string DispatcherId);
