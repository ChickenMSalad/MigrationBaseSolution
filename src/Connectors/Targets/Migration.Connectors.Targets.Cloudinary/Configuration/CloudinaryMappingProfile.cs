using System.Text.Json.Serialization;

namespace Migration.Connectors.Targets.Cloudinary.Configuration;

public sealed class CloudinaryMappingProfile
{
    public CloudinaryDefaultUploadOptions Defaults { get; set; } = new();
    public List<string> FileColumns { get; set; } = new() { "file", "source_url", "s3_url", "Source URL", "URL", "S7_URL" };
    public List<string> PublicIdColumns { get; set; } = new() { "CLD_PublicID", "public_id", "identifier" };
    public List<string> AssetFolderColumns { get; set; } = new() { "CLD_Folder", "cloudinary_folder", "folder" };
    public List<string> DisplayNameColumns { get; set; } = new() { "display_name", "title", "name" };
    public List<string> TagsColumns { get; set; } = new() { "tags" };
    public string TagsSeparator { get; set; } = ",";
    public List<CloudinaryContextMapping> Context { get; set; } = new();
    public List<CloudinaryStructuredMetadataMapping> StructuredMetadata { get; set; } = new();

    public static CloudinaryMappingProfile CreateDefault() => new();
}

public sealed class CloudinaryDefaultUploadOptions
{
    public string ResourceType { get; set; } = "auto";
    public string Type { get; set; } = "upload";
    public string? UploadPreset { get; set; }
    public bool Overwrite { get; set; }
    public bool Invalidate { get; set; }
    public bool UniqueFilename { get; set; }
    public bool UseFilename { get; set; }
}

public sealed class CloudinaryContextMapping
{
    public string Column { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

public sealed class CloudinaryStructuredMetadataMapping
{
    public string Column { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string ValueMode { get; set; } = CloudinaryMetadataValueMode.Direct;
    public string? Separator { get; set; }
    public string? DateInputFormat { get; set; }
    public Dictionary<string, string> StaticOptionMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class CloudinaryMetadataValueMode
{
    public const string Direct = "direct";
    public const string Label = "label";
}
