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

    public async Task<MappingProfile> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var profile = await JsonSerializer.DeserializeAsync<MappingProfile>(stream, SerializerOptions, cancellationToken);
        return profile ?? throw new InvalidOperationException($"Unable to deserialize mapping profile at '{path}'.");
    }
}
