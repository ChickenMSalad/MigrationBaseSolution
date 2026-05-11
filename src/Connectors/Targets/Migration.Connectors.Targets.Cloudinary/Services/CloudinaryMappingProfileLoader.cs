using System.Text.Json;
using Migration.Connectors.Targets.Cloudinary.Configuration;

namespace Migration.Connectors.Targets.Cloudinary.Services;

public sealed class CloudinaryMappingProfileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public CloudinaryMappingProfile Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Cloudinary mapping path is required.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Cloudinary mapping file not found.", path);
        }

        using var stream = File.OpenRead(path);
        var profile = JsonSerializer.Deserialize<CloudinaryMappingProfile>(stream, JsonOptions);
        return profile ?? throw new InvalidOperationException($"Failed to deserialize mapping file '{path}'.");
    }

    public void SaveTemplate(string path)
    {
        var template = CloudinaryMappingProfile.CreateDefault();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(template, JsonOptions));
    }
}
