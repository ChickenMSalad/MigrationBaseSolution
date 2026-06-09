using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
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
                var preview = await PreviewColumnsAsync(artifactId, artifacts, take: 10, cancellationToken)
                    .ConfigureAwait(false);

                return Results.Ok(new
                {
                    preview.ArtifactId,
                    preview.FileName,
                    preview.Columns,
                    preview.SampleRows
                });
            })
            .WithSummary("Read artifact columns and sample rows for the mapping builder.");

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

                if (!string.Equals(mappingType, "intermediate", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(mappingType, "target", StringComparison.OrdinalIgnoreCase))
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
                    : NormalizeIntermediateStorage(request.IntermediateStorage, request.TargetType);

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
                    ["FieldMappingCount"] = cleanedMappings.Count.ToString(CultureInfo.InvariantCulture)
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
                    metadata["BinaryOnly"] = normalizedIntermediateStorage.BinaryOnly.ToString(CultureInfo.InvariantCulture);
                    metadata["BlobTagRuleCount"] = normalizedIntermediateStorage.TagRules.Count.ToString(CultureInfo.InvariantCulture);
                    metadata["MetadataRuleCount"] = normalizedIntermediateStorage.MetadataRules.Count.ToString(CultureInfo.InvariantCulture);

                    if (!string.IsNullOrWhiteSpace(normalizedIntermediateStorage.OutputPath))
                    {
                        metadata["OutputPath"] = normalizedIntermediateStorage.OutputPath;
                    }

                    if (!string.IsNullOrWhiteSpace(normalizedIntermediateStorage.FolderPath))
                    {
                        metadata["FolderPath"] = normalizedIntermediateStorage.FolderPath;
                    }

                    if (!string.IsNullOrWhiteSpace(normalizedIntermediateStorage.TargetRcloneRemoteName))
                    {
                        metadata["TargetRcloneRemoteName"] = normalizedIntermediateStorage.TargetRcloneRemoteName;
                    }
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

                return Results.Created($"/api/artifacts/{artifact.ArtifactId}", new { artifact, mappingProfile });
            })
            .WithSummary("Save a mapping profile as a mapping artifact.");

        return app;
    }

    private static async Task<MappingBuilderArtifactPreview> PreviewColumnsAsync(
        string artifactId,
        IArtifactStore artifacts,
        int take,
        CancellationToken cancellationToken)
    {
        var record = await artifacts.GetAsync(artifactId, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            throw new InvalidOperationException($"Artifact '{artifactId}' was not found.");
        }

        if (record.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            await using var stream = await artifacts.OpenReadAsync(artifactId, cancellationToken).ConfigureAwait(false);
            return ReadXlsxColumns(record.ArtifactId, record.FileName, stream, take);
        }

        try
        {
            var preview = await artifacts.PreviewManifestAsync(artifactId, take, cancellationToken).ConfigureAwait(false);
            var sampleRows = preview.SampleRows
                .Select(row => row.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase))
                .ToList();

            return new MappingBuilderArtifactPreview(
                preview.ArtifactId,
                preview.FileName,
                preview.Columns,
                sampleRows);
        }
        catch when (record.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"'{record.FileName}' is an .xls file. Mapping Builder can preview CSV and .xlsx artifacts. Rebuild the template from Taxonomy Builder so it creates a real .xlsx file.");
        }
    }

    private static MappingBuilderArtifactPreview ReadXlsxColumns(
        string artifactId,
        string fileName,
        Stream stream,
        int take)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var sharedStrings = ReadSharedStrings(archive);
        var worksheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidOperationException($"'{fileName}' does not contain xl/worksheets/sheet1.xml.");

        var rows = ReadWorksheetRows(worksheetEntry, sharedStrings)
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .ToList();

        if (rows.Count == 0)
        {
            return new MappingBuilderArtifactPreview(artifactId, fileName, [], []);
        }

        var columns = rows[0]
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sampleRows = rows
            .Skip(1)
            .Take(take)
            .Select(row => ToSampleRow(columns, row))
            .ToList();

        return new MappingBuilderArtifactPreview(artifactId, fileName, columns, sampleRows);
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return document
            .Descendants(ns + "si")
            .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
            .ToList();
    }

    private static List<List<string>> ReadWorksheetRows(
        ZipArchiveEntry worksheetEntry,
        IReadOnlyList<string> sharedStrings)
    {
        using var stream = worksheetEntry.Open();
        var document = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = new List<List<string>>();

        foreach (var rowElement in document.Descendants(ns + "sheetData").Elements(ns + "row"))
        {
            var valuesByIndex = new SortedDictionary<int, string>();

            foreach (var cell in rowElement.Elements(ns + "c"))
            {
                var reference = cell.Attribute("r")?.Value;
                var columnIndex = ColumnIndexFromCellReference(reference);
                if (columnIndex < 0)
                {
                    columnIndex = valuesByIndex.Count;
                }

                valuesByIndex[columnIndex] = ReadCellValue(cell, sharedStrings, ns);
            }

            if (valuesByIndex.Count == 0)
            {
                rows.Add([]);
                continue;
            }

            var maxIndex = valuesByIndex.Keys.Max();
            var values = Enumerable.Range(0, maxIndex + 1)
                .Select(index => valuesByIndex.TryGetValue(index, out var value) ? value : string.Empty)
                .ToList();

            rows.Add(values);
        }

        return rows;
    }

    private static string ReadCellValue(
        XElement cell,
        IReadOnlyList<string> sharedStrings,
        XNamespace ns)
    {
        var type = cell.Attribute("t")?.Value;
        if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat(cell.Descendants(ns + "t").Select(x => x.Value));
        }

        var rawValue = cell.Element(ns + "v")?.Value ?? string.Empty;
        if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedStringIndex)
            && sharedStringIndex >= 0
            && sharedStringIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedStringIndex];
        }

        return rawValue;
    }

    private static int ColumnIndexFromCellReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return -1;
        }

        var index = 0;
        var foundLetters = false;

        foreach (var ch in reference)
        {
            if (!char.IsLetter(ch))
            {
                break;
            }

            foundLetters = true;
            index = (index * 26) + (char.ToUpperInvariant(ch) - 'A' + 1);
        }

        return foundLetters ? index - 1 : -1;
    }

    private static Dictionary<string, string> ToSampleRow(
        IReadOnlyList<string> columns,
        IReadOnlyList<string> row)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < columns.Count; i++)
        {
            result[columns[i]] = i < row.Count ? row[i] : string.Empty;
        }

        return result;
    }

    private static IntermediateStorageMapping NormalizeIntermediateStorage(
        IntermediateStorageMapping value,
        string targetType)
    {
        var provider = string.IsNullOrWhiteSpace(value.Provider)
            ? (string.Equals(targetType, "LocalStorage", StringComparison.OrdinalIgnoreCase) ? "LocalStorage" : "AzureBlob")
            : value.Provider.Trim();

        if (string.Equals(provider, "LocalStorage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetType, "LocalStorage", StringComparison.OrdinalIgnoreCase))
        {
            return new IntermediateStorageMapping(
                "LocalStorage",
                true,
                string.IsNullOrWhiteSpace(value.BlobNameTemplate) ? "{sourceRelativePath}" : value.BlobNameTemplate.Trim(),
                false,
                false,
                null,
                [],
                [],
                FirstNonBlank(value.OutputPath, value.FolderPath, value.DestinationPath),
                FirstNonBlank(value.FolderPath, value.OutputPath, value.DestinationPath),
                value.PreserveSourceFolderPath,
                FirstNonBlank(value.DestinationPath, value.FolderPath, value.OutputPath),
                FirstNonBlank(value.TargetRcloneRemoteName));
        }

        var binaryOnly = value.BinaryOnly;

        return new IntermediateStorageMapping(
            provider,
            binaryOnly,
            string.IsNullOrWhiteSpace(value.BlobNameTemplate) ? "{assetId}" : value.BlobNameTemplate.Trim(),
            binaryOnly ? false : value.WriteBlobTags,
            binaryOnly ? false : value.WriteMetadataJson,
            binaryOnly || string.IsNullOrWhiteSpace(value.MetadataJsonPathTemplate) ? null : value.MetadataJsonPathTemplate.Trim(),
            binaryOnly ? [] : value.TagRules
                .Where(x => !string.IsNullOrWhiteSpace(x.SourceField) && !string.IsNullOrWhiteSpace(x.TagName))
                .Select(x => new BlobTagRule(
                    x.SourceField.Trim(),
                    x.TagName.Trim(),
                    string.IsNullOrWhiteSpace(x.Transform) ? null : x.Transform.Trim()))
                .ToList(),
            binaryOnly ? [] : value.MetadataRules
                .Where(x => !string.IsNullOrWhiteSpace(x.SourceField) && !string.IsNullOrWhiteSpace(x.JsonPath))
                .Select(x => new MetadataJsonRule(
                    x.SourceField.Trim(),
                    x.JsonPath.Trim(),
                    string.IsNullOrWhiteSpace(x.Transform) ? null : x.Transform.Trim()))
                .ToList(),
            FirstNonBlank(value.OutputPath, value.FolderPath, value.DestinationPath),
            FirstNonBlank(value.FolderPath, value.OutputPath, value.DestinationPath),
            value.PreserveSourceFolderPath,
            FirstNonBlank(value.DestinationPath, value.FolderPath, value.OutputPath),
            FirstNonBlank(value.TargetRcloneRemoteName));
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
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
    [property: JsonPropertyName("metadataRules")] List<MetadataJsonRule> MetadataRules,
    [property: JsonPropertyName("outputPath")] string? OutputPath = null,
    [property: JsonPropertyName("folderPath")] string? FolderPath = null,
    [property: JsonPropertyName("preserveSourceFolderPath")] bool PreserveSourceFolderPath = true,
    [property: JsonPropertyName("destinationPath")] string? DestinationPath = null,
    [property: JsonPropertyName("targetRcloneRemoteName")] string? TargetRcloneRemoteName = null);

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

internal sealed record MappingBuilderArtifactPreview(
    string ArtifactId,
    string FileName,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string>> SampleRows);


