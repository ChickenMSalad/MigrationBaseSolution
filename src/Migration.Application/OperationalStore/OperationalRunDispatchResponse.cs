namespace Migration.Application.OperationalStore;

public sealed class OperationalRunDispatchResponse
{
    public Guid RunId { get; init; }

    public int ManifestRecordCount { get; init; }

    public int PublishedQueueMessageCount { get; init; }

    public IReadOnlyCollection<OperationalManifestDispatchResponseItem> Items { get; init; } =
        Array.Empty<OperationalManifestDispatchResponseItem>();
}
