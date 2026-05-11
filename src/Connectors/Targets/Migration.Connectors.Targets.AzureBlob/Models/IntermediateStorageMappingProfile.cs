using System.Text.Json;
using System.Text.Json.Serialization;

namespace Migration.Connectors.Targets.AzureBlob.Models;

/// <summary>
/// Mapping Builder intermediate-storage profile payload.
/// This intentionally mirrors the JSON emitted by MappingBuilderEndpoints.cs.
/// </summary>
public sealed record IntermediateStorageMappingProfile(
    [property: JsonPropertyName("profileName")] string? ProfileName,
    [property: JsonPropertyName("sourceType")] string? SourceType,
    [property: JsonPropertyName("targetType")] string? TargetType,
    [property: JsonPropertyName("mappingType")] string? MappingType,
    [property: JsonPropertyName("intermediateStorage")] IntermediateStorageOptions? IntermediateStorage,
    [property: JsonPropertyName("fieldMappings")] List<IntermediateFieldMapping>? FieldMappings = null,
    [property: JsonPropertyName("requiredTargetFields")] List<string>? RequiredTargetFields = null);

public sealed record IntermediateStorageOptions(
    [property: JsonPropertyName("provider")] string? Provider,
    [property: JsonPropertyName("binaryOnly")] bool BinaryOnly,
    [property: JsonPropertyName("blobNameTemplate")] string? BlobNameTemplate,
    [property: JsonPropertyName("writeBlobTags")] bool WriteBlobTags,
    [property: JsonPropertyName("writeMetadataJson")] bool WriteMetadataJson,
    [property: JsonPropertyName("metadataJsonPathTemplate")] string? MetadataJsonPathTemplate,
    [property: JsonPropertyName("tagRules")] List<BlobTagRule>? TagRules,
    [property: JsonPropertyName("metadataRules")] List<MetadataJsonRule>? MetadataRules);

public sealed record IntermediateFieldMapping(
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("target")] string? Target,
    [property: JsonPropertyName("transform")] string? Transform);

public sealed record BlobTagRule(
    [property: JsonPropertyName("sourceField")] string? SourceField,
    [property: JsonPropertyName("tagName")] string? TagName,
    [property: JsonPropertyName("transform")] string? Transform);

public sealed record MetadataJsonRule(
    [property: JsonPropertyName("sourceField")] string? SourceField,
    [property: JsonPropertyName("jsonPath")] string? JsonPath,
    [property: JsonPropertyName("transform")] string? Transform);

public static class IntermediateStorageMappingProfileReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static async Task<IntermediateStorageMappingProfile?> ReadAsync(
        string? mappingProfilePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mappingProfilePath) || !File.Exists(mappingProfilePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(mappingProfilePath);
        return await JsonSerializer.DeserializeAsync<IntermediateStorageMappingProfile>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);
    }

    public static IntermediateStorageOptions? GetIntermediateStorageOrNull(IntermediateStorageMappingProfile? profile)
    {
        if (profile is null)
        {
            return null;
        }

        if (!string.Equals(profile.MappingType, "intermediate", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (profile.IntermediateStorage is null)
        {
            return null;
        }

        if (!string.Equals(profile.IntermediateStorage.Provider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return profile.IntermediateStorage;
    }
}
