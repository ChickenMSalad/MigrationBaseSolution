using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Migration.ControlPlane.Artifacts;

namespace Migration.Admin.Api.Endpoints;

public static class MappingBuilderEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static IEndpointRouteBuilder MapMappingBuilderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mapping-builder")
            .WithTags("Mapping Builder");

        group.MapGet("/manifests/{artifactId}/columns", async (
                string artifactId,
                IArtifactStore artifacts,
                CancellationToken cancellationToken) =>
            {
                var preview = await artifacts.PreviewManifestAsync(artifactId, take: 10, cancellationToken).ConfigureAwait(false);
                return Results.Ok(new
                {
                    preview.ArtifactId,
                    preview.FileName,
                    preview.Columns,
                    preview.SampleRows
                });
            })
            .WithSummary("Read manifest columns and sample rows for the mapping builder.");

        group.MapPost("/mappings", async (
                SaveMappingArtifactRequest request,
                IArtifactStore artifacts,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.ProfileName))
                {
                    return Results.BadRequest(new { error = "profileName is required." });
                }

                if (string.IsNullOrWhiteSpace(request.SourceType))
                {
                    return Results.BadRequest(new { error = "sourceType is required." });
                }

                if (string.IsNullOrWhiteSpace(request.TargetType))
                {
                    return Results.BadRequest(new { error = "targetType is required." });
                }

                var mappingType = string.IsNullOrWhiteSpace(request.MappingType)
                    ? "target"
                    : request.MappingType.Trim();

                if (!string.Equals(mappingType, "intermediate", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(mappingType, "target", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "mappingType must be either 'intermediate' or 'target'." });
                }

                var cleanedMappings = request.FieldMappings
                    .Where(x => !string.IsNullOrWhiteSpace(x.Source) && !string.IsNullOrWhiteSpace(x.Target))
                    .Select(x => new MappingBuilderFieldMap(
                        x.Source.Trim(),
                        x.Target.Trim(),
                        string.IsNullOrWhiteSpace(x.Transform) ? null : x.Transform.Trim()))
                    .ToList();

                if (string.Equals(mappingType, "target", StringComparison.OrdinalIgnoreCase) && cleanedMappings.Count == 0)
                {
                    return Results.BadRequest(new { error = "At least one field mapping with source and target is required for target mappings." });
                }

                if (string.Equals(mappingType, "intermediate", StringComparison.OrdinalIgnoreCase) && request.IntermediateStorage is null)
                {
                    return Results.BadRequest(new { error = "intermediateStorage is required for intermediate mappings." });
                }

                var requiredTargetFields = request.RequiredTargetFields
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var normalizedIntermediateStorage = request.IntermediateStorage is null
                    ? null
                    : NormalizeIntermediateStorage(request.IntermediateStorage);

                var mappingProfile = new MappingProfileDocument(
                    request.ProfileName.Trim(),
                    request.SourceType.Trim(),
                    request.TargetType.Trim(),
                    mappingType,
                    cleanedMappings,
                    requiredTargetFields,
                    normalizedIntermediateStorage,
                    string.IsNullOrWhiteSpace(request.ManifestArtifactId) ? null : request.ManifestArtifactId.Trim(),
                    string.IsNullOrWhiteSpace(request.TargetArtifactId) ? null : request.TargetArtifactId.Trim());

                var json = JsonSerializer.Serialize(mappingProfile, JsonOptions);
                await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

                var fileName = string.IsNullOrWhiteSpace(request.FileName)
                    ? MakeSafeFileName($"{request.ProfileName.Trim()}.mapping.json")
                    : MakeSafeFileName(request.FileName.Trim());

                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".json";
                }

                var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["GeneratedBy"] = "MappingBuilder",
                    ["SourceType"] = mappingProfile.SourceType,
                    ["TargetType"] = mappingProfile.TargetType,
                    ["MappingType"] = mappingProfile.MappingType,
                    ["FieldMappingCount"] = cleanedMappings.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                };

                if (!string.IsNullOrWhiteSpace(request.ManifestArtifactId))
                {
                    metadata["ManifestArtifactId"] = request.ManifestArtifactId.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.TargetArtifactId))
                {
                    metadata["TargetArtifactId"] = request.TargetArtifactId.Trim();
                }

                if (normalizedIntermediateStorage is not null)
                {
                    metadata["IntermediateProvider"] = normalizedIntermediateStorage.Provider;
                    metadata["BinaryOnly"] = normalizedIntermediateStorage.BinaryOnly.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    metadata["BlobTagRuleCount"] = normalizedIntermediateStorage.TagRules.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    metadata["MetadataRuleCount"] = normalizedIntermediateStorage.MetadataRules.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                var artifact = await artifacts.SaveAsync(
                    stream,
                    fileName,
                    "application/json",
                    ArtifactKind.Mapping,
                    request.ProjectId,
                    string.IsNullOrWhiteSpace(request.Description) ? "Generated by Mapping Builder" : request.Description,
                    metadata,
                    cancellationToken).ConfigureAwait(false);

                return Results.Created($"/api/artifacts/{artifact.ArtifactId}", new
                {
                    artifact,
                    mappingProfile
                });
            })
            .WithSummary("Save a mapping profile as a mapping artifact.");

        return app;
    }

    private static IntermediateStorageMapping NormalizeIntermediateStorage(IntermediateStorageMapping value)
    {
        var binaryOnly = value.BinaryOnly;

        return new IntermediateStorageMapping(
            string.IsNullOrWhiteSpace(value.Provider) ? "AzureBlob" : value.Provider.Trim(),
            binaryOnly,
            string.IsNullOrWhiteSpace(value.BlobNameTemplate) ? "{assetId}" : value.BlobNameTemplate.Trim(),
            binaryOnly ? false : value.WriteBlobTags,
            binaryOnly ? false : value.WriteMetadataJson,
            binaryOnly || string.IsNullOrWhiteSpace(value.MetadataJsonPathTemplate) ? null : value.MetadataJsonPathTemplate.Trim(),
            binaryOnly
                ? []
                : value.TagRules
                    .Where(x => !string.IsNullOrWhiteSpace(x.SourceField) && !string.IsNullOrWhiteSpace(x.TagName))
                    .Select(x => new BlobTagRule(
                        x.SourceField.Trim(),
                        x.TagName.Trim(),
                        string.IsNullOrWhiteSpace(x.Transform) ? null : x.Transform.Trim()))
                    .ToList(),
            binaryOnly
                ? []
                : value.MetadataRules
                    .Where(x => !string.IsNullOrWhiteSpace(x.SourceField) && !string.IsNullOrWhiteSpace(x.JsonPath))
                    .Select(x => new MetadataJsonRule(
                        x.SourceField.Trim(),
                        x.JsonPath.Trim(),
                        string.IsNullOrWhiteSpace(x.Transform) ? null : x.Transform.Trim()))
                    .ToList());
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? $"mapping-{Guid.NewGuid():N}.json" : safe;
    }
}

public sealed record SaveMappingArtifactRequest(
    string ProfileName,
    string SourceType,
    string TargetType,
    List<MappingBuilderFieldMap> FieldMappings,
    List<string> RequiredTargetFields,
    string? MappingType = null,
    IntermediateStorageMapping? IntermediateStorage = null,
    string? ManifestArtifactId = null,
    string? TargetArtifactId = null,
    string? ProjectId = null,
    string? FileName = null,
    string? Description = null);

public sealed record MappingBuilderFieldMap(
    string Source,
    string Target,
    string? Transform = null);

public sealed record BlobTagRule(
    string SourceField,
    string TagName,
    string? Transform = null);

public sealed record MetadataJsonRule(
    string SourceField,
    string JsonPath,
    string? Transform = null);

public sealed record IntermediateStorageMapping(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("binaryOnly")] bool BinaryOnly,
    [property: JsonPropertyName("blobNameTemplate")] string BlobNameTemplate,
    [property: JsonPropertyName("writeBlobTags")] bool WriteBlobTags,
    [property: JsonPropertyName("writeMetadataJson")] bool WriteMetadataJson,
    [property: JsonPropertyName("metadataJsonPathTemplate")] string? MetadataJsonPathTemplate,
    [property: JsonPropertyName("tagRules")] List<BlobTagRule> TagRules,
    [property: JsonPropertyName("metadataRules")] List<MetadataJsonRule> MetadataRules);

public sealed record MappingProfileDocument(
    [property: JsonPropertyName("profileName")] string ProfileName,
    [property: JsonPropertyName("sourceType")] string SourceType,
    [property: JsonPropertyName("targetType")] string TargetType,
    [property: JsonPropertyName("mappingType")] string MappingType,
    [property: JsonPropertyName("fieldMappings")] List<MappingBuilderFieldMap> FieldMappings,
    [property: JsonPropertyName("requiredTargetFields")] List<string> RequiredTargetFields,
    [property: JsonPropertyName("intermediateStorage")] IntermediateStorageMapping? IntermediateStorage = null,
    [property: JsonPropertyName("manifestArtifactId")] string? ManifestArtifactId = null,
    [property: JsonPropertyName("targetArtifactId")] string? TargetArtifactId = null);
