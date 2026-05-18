using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalRunDispatchRequestHandler : IOperationalRunDispatchRequestHandler
{
    private readonly IOperationalRunDispatchCommandService _commandService;

    public OperationalRunDispatchRequestHandler(
        IOperationalRunDispatchCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<OperationalRunDispatchResponse> HandleAsync(
        OperationalRunDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _commandService.DispatchAsync(
            new OperationalRunDispatchCommand
            {
                SourceSystem = request.SourceSystem,
                TargetSystem = request.TargetSystem,
                ManifestRecords = request.ManifestRecords
            },
            cancellationToken);

        return new OperationalRunDispatchResponse
        {
            RunId = result.Run.RunId,
            ManifestRecordCount = result.ManifestRecordCount,
            PublishedQueueMessageCount = result.PublishedQueueMessageCount,
            Items = result.ManifestDispatchResults
                .Select(item => new OperationalManifestDispatchResponseItem
                {
                    ManifestRecordId = item.ManifestRecord.ManifestRecordId,
                    WorkItemId = item.QueueMessage?.WorkItemId,
                    SourceId = item.ManifestRecord.SourceId,
                    SourcePath = item.ManifestRecord.SourcePath,
                    SourceName = item.ManifestRecord.SourceName,
                    QueueMessagePublished = item.QueueMessage is not null
                })
                .ToArray()
        };
    }
}
