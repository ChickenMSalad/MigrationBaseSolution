namespace Migration.Workers.ServiceBusDispatcher.Dispatching;

internal sealed record SqlWorkItemDispatchRecord(
    Guid WorkItemId,
    Guid RunId,
    long SequenceNumber,
    string Status,
    string? PayloadJson);
