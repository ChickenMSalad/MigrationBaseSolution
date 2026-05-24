namespace Migration.Workers.ServiceBusDispatcher.Dispatching;

internal sealed record ServiceBusWorkItemMessage(
    long WorkItemId,
    Guid RunId,
    long SequenceNumber,
    string? PayloadJson,
    DateTimeOffset DispatchedAtUtc,
    string DispatcherId);
