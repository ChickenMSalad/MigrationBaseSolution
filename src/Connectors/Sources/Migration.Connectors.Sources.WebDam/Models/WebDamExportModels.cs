using System.Collections.Generic;

namespace Migration.Connectors.Sources.WebDam.Models;

public sealed class WebDamAssetExportRow
{
    public string AssetId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Name { get; set; }
    public long? SizeBytes { get; set; }
    public string? FileType { get; set; }
    public string FolderId { get; set; } = string.Empty;
    public string FolderPath { get; set; } = "/";
}

public sealed class WebDamMetadataExportRow
{
    public string AssetId { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);
}

public sealed class WebDamMetadataSchemaExportRow
{
    public string Field { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Label { get; set; }
    public string? Status { get; set; }
    public string? Searchable { get; set; }
    public string? Position { get; set; }
    public string? Type { get; set; }
    public string? PossibleValues { get; set; }
}

public sealed class WebDamExportResult
{
    public IReadOnlyList<WebDamAssetExportRow> Assets { get; init; } = new List<WebDamAssetExportRow>();

    public IReadOnlyList<WebDamMetadataExportRow> MetadataRows { get; init; } = new List<WebDamMetadataExportRow>();

    public IReadOnlyList<WebDamMetadataSchemaExportRow> MetadataSchemaRows { get; init; } = new List<WebDamMetadataSchemaExportRow>();

    public IReadOnlyDictionary<string, string> MetadataDisplayNames { get; init; } =
        new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
}
