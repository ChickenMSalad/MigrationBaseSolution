using System.Globalization;
using System.Text;
using Migration.Connectors.Sources.WebDam.Models;

namespace Migration.Connectors.Sources.WebDam.ManifestBuilder;

public static class WebDamManifestCsvWriter
{
    private static readonly string[] AssetColumns =
    [
        "Asset Id",
        "File Name",
        "Asset Name",
        "Size Bytes",
        "File Type",
        "Folder Id",
        "Folder Path"
    ];

    public static string WriteManifest(WebDamExportResult export)
    {
        var metadataFields = export.MetadataSchemaRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Field))
            .Select(row => row.Field)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(field => BuildMetadataHeader(field!, export.MetadataDisplayNames), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var metadataByAssetId = export.MetadataRows
            .Where(row => !string.IsNullOrWhiteSpace(row.AssetId))
            .ToDictionary(row => row.AssetId, StringComparer.OrdinalIgnoreCase);

        var headers = AssetColumns
            .Concat(metadataFields.Select(field => BuildMetadataHeader(field!, export.MetadataDisplayNames)))
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(Escape)));

        foreach (var asset in export.Assets
            .Where(asset => !string.IsNullOrWhiteSpace(asset.AssetId))
            .OrderBy(asset => asset.FolderPath ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(asset => asset.FileName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(asset => asset.AssetId, StringComparer.OrdinalIgnoreCase))
        {
            metadataByAssetId.TryGetValue(asset.AssetId, out var metadataRow);

            var values = new List<string>
            {
                asset.AssetId,
                asset.FileName,
                asset.Name ?? string.Empty,
                Format(asset.SizeBytes),
                asset.FileType ?? string.Empty,
                asset.FolderId,
                asset.FolderPath ?? "/"
            };

            foreach (var field in metadataFields)
            {
                var value = metadataRow is not null &&
                            metadataRow.Metadata.TryGetValue(field!, out var metadataValue)
                    ? metadataValue
                    : string.Empty;

                values.Add(value ?? string.Empty);
            }

            builder.AppendLine(string.Join(",", values.Select(Escape)));
        }

        return builder.ToString();
    }

    private static string BuildMetadataHeader(
        string field,
        IReadOnlyDictionary<string, string> displayNames)
    {
        if (!displayNames.TryGetValue(field, out var displayName) ||
            string.IsNullOrWhiteSpace(displayName) ||
            string.Equals(displayName, field, StringComparison.OrdinalIgnoreCase))
        {
            return field;
        }

        return $"{displayName} ({field})";
    }

    private static string Format(long? value)
    {
        return value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        var mustQuote = text.Contains(',') || text.Contains('"') || text.Contains('\r') || text.Contains('\n');

        if (text.Contains('"'))
        {
            text = text.Replace("\"", "\"\"");
        }

        return mustQuote ? $"\"{text}\"" : text;
    }
}