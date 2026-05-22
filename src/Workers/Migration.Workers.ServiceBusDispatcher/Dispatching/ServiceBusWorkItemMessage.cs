namespace Migration.Workers.ServiceBusDispatcher.Dispatching;

internal sealed record ServiceBusWorkItemMessage(
    Guid WorkItemId,
    Guid RunId,
    long SequenceNumber,
    string? PayloadJson,
    DateTimeOffset DispatchedAtUtc,
    string DispatcherId);
