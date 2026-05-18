using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalManifestDispatchService : IOperationalManifestDispatchService
{
    private readonly IOperationalManifestLifecycleService _manifestLifecycleService;
    private readonly IOperationalWorkItemDispatchService _workItemDispatchService;

    public OperationalManifestDispatchService(
        IOperationalManifestLifecycleService manifestLifecycleService,
        IOperationalWorkItemDispatchService workItemDispatchService)
    {
        _manifestLifecycleService = manifestLifecycleService;
        _workItemDispatchService = workItemDispatchService;
    }

    public async Task<OperationalManifestDispatchResult> DispatchAsync(
        Guid runId,
        long sequenceNumber,
        string sourceId,
        string? sourcePath,
        string? sourceName,
        string? contentType,
        long? contentLength,
        CancellationToken cancellationToken = default)
    {
        var manifestRecord = await _manifestLifecycleService.CreateManifestRecordAsync(
            runId,
            sequenceNumber,
            sourceId,
            sourcePath,
            sourceName,
            contentType,
            contentLength,
            cancellationToken);

        var queueMessage = await _workItemDispatchService.DispatchAsync(
            runId,
            manifestRecord.ManifestRecordId,
            cancellationToken);

        return new OperationalManifestDispatchResult(
            manifestRecord,
            queueMessage);
    }

    public async Task<IReadOnlyList<OperationalManifestDispatchResult>> DispatchBatchAsync(
        Guid runId,
        IReadOnlyCollection<MigrationManifestRecord> manifestRecords,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifestRecords);

        if (manifestRecords.Count == 0)
        {
            return Array.Empty<OperationalManifestDispatchResult>();
        }

        var persistedManifestRecords = await _manifestLifecycleService.CreateManifestBatchAsync(
            runId,
            manifestRecords,
            cancellationToken);

        var queueMessages = await _workItemDispatchService.DispatchBatchAsync(
            runId,
            persistedManifestRecords
                .Select(record => record.ManifestRecordId)
                .ToArray(),
            cancellationToken);

        var queueMessageByManifestRecordId = queueMessages
            .GroupBy(message => message.ManifestRecordId)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        return persistedManifestRecords
            .Select(record => new OperationalManifestDispatchResult(
                record,
                queueMessageByManifestRecordId.TryGetValue(record.ManifestRecordId, out var queueMessage)
                    ? queueMessage
                    : null))
            .ToArray();
    }
}
