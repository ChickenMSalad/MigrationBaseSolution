namespace Migration.Application.OperationalStore;

public sealed class OperationalRunDispatchRequest
{
    public string SourceSystem { get; init; } = string.Empty;

    public string TargetSystem { get; init; } = string.Empty;

    public IReadOnlyCollection<OperationalManifestRecordInput> ManifestRecords { get; init; } =
        Array.Empty<OperationalManifestRecordInput>();
}
