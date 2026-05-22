namespace Migration.ControlPlane.ManifestBuilder;

public interface ISourceManifestService
{
    string SourceType { get; }

    string ServiceName { get; }

    ManifestBuilderServiceDescriptor GetDescriptor();

    Task<BuildSourceManifestResult> BuildAsync(
        BuildSourceManifestRequest request,
        CancellationToken cancellationToken = default);
}
