namespace Migration.Connectors.Targets.Cloudinary.Models;

public sealed class CloudinaryMetadataFieldSchema
{
    public string ExternalId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public List<CloudinaryMetadataDatasourceValue> DatasourceValues { get; init; } = new();
}

public sealed class CloudinaryMetadataDatasourceValue
{
    public string ExternalId { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
