using Migration.Application.Models.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalRunDispatchResult
{
    public OperationalRunDispatchResult(
        MigrationRunRecord run,
        IReadOnlyList<OperationalManifestDispatchResult> manifestDispatchResults)
    {
        Run = run;
        ManifestDispatchResults = manifestDispatchResults;
    }

    public MigrationRunRecord Run { get; }

    public IReadOnlyList<OperationalManifestDispatchResult> ManifestDispatchResults { get; }

    public int ManifestRecordCount => ManifestDispatchResults.Count;

    public int PublishedQueueMessageCount =>
        ManifestDispatchResults.Count(result => result.QueueMessage is not null);
}
