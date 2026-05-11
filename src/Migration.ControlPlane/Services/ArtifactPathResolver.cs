using Migration.ControlPlane.Artifacts;
using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

public sealed class ArtifactPathResolver
{
    private readonly IArtifactStore _artifacts;

    public ArtifactPathResolver(IArtifactStore artifacts)
    {
        _artifacts = artifacts;
    }

    public async Task<CreateRunRequest> ResolveRunRequestAsync(
        MigrationProjectRecord project,
        CreateRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var manifestArtifactId = FirstNonEmpty(request.ManifestArtifactId, project.ManifestArtifactId);
        var mappingArtifactId = FirstNonEmpty(request.MappingArtifactId, project.MappingArtifactId);

        var manifestPath = await ResolvePathAsync(
            explicitPath: request.ManifestPath,
            artifactId: manifestArtifactId,
            expectedKind: ArtifactKind.Manifest,
            logicalName: "manifest",
            cancellationToken).ConfigureAwait(false);

        var mappingPath = await ResolvePathAsync(
            explicitPath: request.MappingProfilePath,
            artifactId: mappingArtifactId,
            expectedKind: ArtifactKind.Mapping,
            logicalName: "mapping profile",
            cancellationToken).ConfigureAwait(false);

        var settings = AddArtifactSettings(request.Settings, manifestArtifactId, mappingArtifactId);

        return request with
        {
            ManifestPath = manifestPath,
            MappingProfilePath = mappingPath,
            ManifestArtifactId = manifestArtifactId,
            MappingArtifactId = mappingArtifactId,
            Settings = settings
        };
    }

    public async Task<CreatePreflightRequest> ResolvePreflightRequestAsync(
        MigrationProjectRecord project,
        CreatePreflightRequest request,
        CancellationToken cancellationToken = default)
    {
        var manifestArtifactId = FirstNonEmpty(request.ManifestArtifactId, project.ManifestArtifactId);
        var mappingArtifactId = FirstNonEmpty(request.MappingArtifactId, project.MappingArtifactId);

        var manifestPath = await ResolvePathAsync(
            explicitPath: request.ManifestPath,
            artifactId: manifestArtifactId,
            expectedKind: ArtifactKind.Manifest,
            logicalName: "manifest",
            cancellationToken).ConfigureAwait(false);

        var mappingPath = await ResolvePathAsync(
            explicitPath: request.MappingProfilePath,
            artifactId: mappingArtifactId,
            expectedKind: ArtifactKind.Mapping,
            logicalName: "mapping profile",
            cancellationToken).ConfigureAwait(false);

        var settings = AddArtifactSettings(request.Settings, manifestArtifactId, mappingArtifactId);

        return request with
        {
            ManifestPath = manifestPath,
            MappingProfilePath = mappingPath,
            ManifestArtifactId = manifestArtifactId,
            MappingArtifactId = mappingArtifactId,
            Settings = settings
        };
    }

    public async Task<ProjectArtifactBindingResponse> GetBindingAsync(
        MigrationProjectRecord project,
        CancellationToken cancellationToken = default)
    {
        var manifestPath = await TryGetArtifactPathAsync(project.ManifestArtifactId, cancellationToken).ConfigureAwait(false);
        var mappingPath = await TryGetArtifactPathAsync(project.MappingArtifactId, cancellationToken).ConfigureAwait(false);

        return new ProjectArtifactBindingResponse(
            project.ProjectId,
            project.ManifestArtifactId,
            manifestPath,
            project.MappingArtifactId,
            mappingPath);
    }

    private async Task<string> ResolvePathAsync(
        string? explicitPath,
        string? artifactId,
        ArtifactKind expectedKind,
        string logicalName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return explicitPath.Trim();
        }

        if (string.IsNullOrWhiteSpace(artifactId))
        {
            throw new InvalidOperationException(
                $"A {logicalName} path or {logicalName} artifact id is required.");
        }

        var artifact = await _artifacts.GetAsync(artifactId.Trim(), cancellationToken).ConfigureAwait(false)
            ?? throw new FileNotFoundException($"The {logicalName} artifact '{artifactId}' was not found.");

        if (artifact.Kind != expectedKind)
        {
            throw new InvalidOperationException(
                $"Artifact '{artifactId}' is '{artifact.Kind}', but a '{expectedKind}' artifact is required for the {logicalName}.");
        }

        if (string.IsNullOrWhiteSpace(artifact.AbsolutePath) || !File.Exists(artifact.AbsolutePath))
        {
            throw new FileNotFoundException(
                $"The file for artifact '{artifactId}' was not found at '{artifact.AbsolutePath}'.");
        }

        return artifact.AbsolutePath;
    }

    private async Task<string?> TryGetArtifactPathAsync(string? artifactId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            return null;
        }

        var artifact = await _artifacts.GetAsync(artifactId.Trim(), cancellationToken).ConfigureAwait(false);
        return artifact?.AbsolutePath;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();

    private static Dictionary<string, string?>? AddArtifactSettings(
        Dictionary<string, string?>? existing,
        string? manifestArtifactId,
        string? mappingArtifactId)
    {
        if (string.IsNullOrWhiteSpace(manifestArtifactId) && string.IsNullOrWhiteSpace(mappingArtifactId))
        {
            return existing;
        }

        var settings = new Dictionary<string, string?>(existing ?? new(), StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(manifestArtifactId))
        {
            settings["ManifestArtifactId"] = manifestArtifactId;
        }

        if (!string.IsNullOrWhiteSpace(mappingArtifactId))
        {
            settings["MappingArtifactId"] = mappingArtifactId;
        }

        return settings;
    }
}
