using System.Text.Json.Serialization;

namespace Migration.Application.Models;

public sealed class MappingProfile
{
    [JsonPropertyName("profileName")]
    public required string ProfileName { get; init; }

    [JsonPropertyName("sourceType")]
    public required string SourceType { get; init; }

    [JsonPropertyName("targetType")]
    public required string TargetType { get; init; }

    [JsonPropertyName("fieldMappings")]
    public List<FieldMap> FieldMappings { get; init; } = new();

    [JsonPropertyName("requiredTargetFields")]
    public List<string> RequiredTargetFields { get; init; } = new();
}

public sealed class FieldMap
{
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("transform")]
    public string? Transform { get; init; }
}
