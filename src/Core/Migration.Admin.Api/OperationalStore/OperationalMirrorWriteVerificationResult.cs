namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorWriteVerificationResult
{
    public bool HasRuns { get; init; }

    public bool HasManifestRecords { get; init; }

    public bool HasWorkItems { get; init; }

    public bool HasCheckpoints { get; init; }

    public int RunCount { get; init; }

    public int ManifestRecordCount { get; init; }

    public int WorkItemCount { get; init; }

    public int CheckpointCount { get; init; }

    public bool HasMirrorWrites =>
        HasRuns &&
        HasManifestRecords &&
        HasWorkItems &&
        HasCheckpoints;

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}
