using System.Text.Json;
using Migration.Application.Abstractions;
using Migration.Application.Models;

namespace Migration.Infrastructure.Profiles;

public sealed class JsonMappingProfileLoader : IMappingProfileLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IArtifactContentResolver? _artifactContentResolver;

    public JsonMappingProfileLoader(IArtifactContentResolver? artifactContentResolver = null)
    {
        _artifactContentResolver = artifactContentResolver;
    }

    public async Task<MappingProfile> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Mapping profile path is required.", nameof(path));
        }

        if (_artifactContentResolver is not null && _artifactContentResolver.IsArtifactReference(path))
        {
            await using var artifact = await _artifactContentResolver
                .OpenReadAsync(path, cancellationToken)
                .ConfigureAwait(false);

            var profileFromArtifact = await JsonSerializer.DeserializeAsync<MappingProfile>(
                    artifact.Content,
                    SerializerOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return profileFromArtifact
                ?? throw new InvalidOperationException($"Unable to deserialize mapping profile artifact '{path}'.");
        }

        await using var stream = File.OpenRead(path);
        var profile = await JsonSerializer.DeserializeAsync<MappingProfile>(
                stream,
                SerializerOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return profile ?? throw new InvalidOperationException($"Unable to deserialize mapping profile at '{path}'.");
    }
}
