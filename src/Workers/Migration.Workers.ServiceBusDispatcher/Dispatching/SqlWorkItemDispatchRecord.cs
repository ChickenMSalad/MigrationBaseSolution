namespace Migration.Workers.ServiceBusDispatcher.Dispatching;

internal sealed record SqlWorkItemDispatchRecord(
    long WorkItemId,
    Guid RunId,
    long SequenceNumber,
    string Status,
    string? PayloadJson);
