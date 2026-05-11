namespace Migration.Connectors.Targets.Cloudinary.Models;

public sealed class CloudinaryUploadRequest
{
    public required string File { get; init; }
    public string? PublicId { get; init; }
    public string? AssetFolder { get; init; }
    public string ResourceType { get; init; } = "auto";
    public string Type { get; init; } = "upload";
    public string? UploadPreset { get; init; }
    public bool Overwrite { get; init; }
    public bool Invalidate { get; init; }
    public bool UniqueFilename { get; init; }
    public bool UseFilename { get; init; }
    public List<string> Tags { get; init; } = new();
    public Dictionary<string, string> Context { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, object> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
