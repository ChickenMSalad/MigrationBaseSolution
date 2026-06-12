namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorRunDetailResponse
{
    public OperationalMirrorRunSummary Run { get; init; } = default!;

    public IReadOnlyCollection<OperationalMirrorManifestRecordItem> ManifestRecords { get; init; } =
        Array.Empty<OperationalMirrorManifestRecordItem>();

    public IReadOnlyCollection<OperationalMirrorWorkItemItem> WorkItems { get; init; } =
        Array.Empty<OperationalMirrorWorkItemItem>();

    public IReadOnlyCollection<OperationalMirrorCheckpointItem> Checkpoints { get; init; } =
        Array.Empty<OperationalMirrorCheckpointItem>();
}


