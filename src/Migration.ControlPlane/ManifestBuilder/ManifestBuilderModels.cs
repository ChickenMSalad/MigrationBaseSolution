namespace Migration.ControlPlane.ManifestBuilder;

public sealed record ManifestBuilderOptionDescriptor(
    string Name,
    string Label,
    string? Description = null,
    bool Required = false,
    string? Placeholder = null);

public sealed record ManifestBuilderServiceDescriptor(
    string SourceType,
    string ServiceName,
    string DisplayName,
    string? Description,
    IReadOnlyList<ManifestBuilderOptionDescriptor> Options);

public sealed record ManifestBuilderSourceDescriptor(
    string SourceType,
    string DisplayName,
    IReadOnlyList<ManifestBuilderServiceDescriptor> Services);

public sealed record BuildSourceManifestRequest(
    string SourceType,
    string ServiceName,
    string? CredentialSetId,
    Dictionary<string, string>? Options);

public sealed record BuildSourceManifestResult(
    string SourceType,
    string ServiceName,
    string FileName,
    string ContentType,
    string Content,
    int RowCount);

public sealed record BuildSourceManifestResponse(
    string ManifestId,
    string SourceType,
    string ServiceName,
    string FileName,
    string ContentType,
    int RowCount,
    string DownloadUrl,
    DateTimeOffset CreatedUtc);
