namespace Migration.Connectors.Targets.AzureBlob.Configuration;

public sealed class AzureBlobTargetOptions
{
    public const string SectionName = "AzureBlobTarget";

    public string? ConnectionString { get; init; }
    public string? ContainerName { get; init; }

    /// <summary>
    /// Optional root prefix before the DAM folder path, e.g. "webdam-export/ntara".
    /// </summary>
    public string? RootFolderPath { get; init; }

    /// <summary>
    /// Backward-compatible alias. Used only when RootFolderPath is not supplied.
    /// </summary>
    public string? FolderPath { get; init; }

    public bool CreateContainerIfMissing { get; init; } = true;
    public bool Overwrite { get; init; } = false;

    /// <summary>
    /// Column/key containing the DAM folder path. For the attached WebDam sample this is "Folder Path".
    /// </summary>
    public string SourceFolderPathField { get; init; } = "Folder Path";

    /// <summary>
    /// Column/key containing the unique asset id used in file prefixes.
    /// For WebDam this is usually webdam_id or Asset Id.
    /// </summary>
    public string UniqueIdField { get; init; } = "webdam_id";

    /// <summary>
    /// Column/key containing the filename from the DAM manifest.
    /// </summary>
    public string FileNameField { get; init; } = "File Name";

    /// <summary>
    /// Template for the stored binary filename. Supported tokens:
    /// {uniqueid}, {filename}, {assetname}, {rowid}, {extension}, {basename}.
    /// </summary>
    public string BinaryFileNameTemplate { get; init; } = "{uniqueid}_{filename}";

    /// <summary>
    /// Template for the metadata sidecar filename. Same tokens as BinaryFileNameTemplate.
    /// </summary>
    public string MetadataFileNameTemplate { get; init; } = "{uniqueid}_metadata.json";

    /// <summary>
    /// When true, use the DAM folder path underneath RootFolderPath.
    /// </summary>
    public bool PreserveSourceFolderPath { get; init; } = true;

    /// <summary>
    /// Writes a JSON metadata document next to the binary.
    /// </summary>
    public bool WriteMetadataSidecar { get; init; } = true;

    /// <summary>
    /// Controls which metadata values are written into the sidecar.
    /// Valid values: All, MappedOnly, ManifestOnly, SourceEnvelopeOnly, None.
    /// </summary>
    public string MetadataSidecarMode { get; init; } = "All";

    /// <summary>
    /// Optional comma/semicolon separated allow-list of metadata columns for the sidecar.
    /// Empty means include all columns for the selected mode.
    /// </summary>
    public string? MetadataIncludeColumns { get; init; }

    /// <summary>
    /// Optional comma/semicolon separated deny-list of metadata columns for the sidecar.
    /// </summary>
    public string? MetadataExcludeColumns { get; init; }

    public bool IncludeEmptyMetadataValues { get; init; } = false;
    public bool PrettyPrintMetadataSidecar { get; init; } = true;

    /// <summary>
    /// Azure blob metadata on the binary itself. Keep this minimal by default because Azure metadata has strict key/value rules.
    /// </summary>
    public bool WriteBlobMetadata { get; init; } = true;
    public bool IncludeMappedFieldsAsBlobMetadata { get; init; } = false;
    public bool IncludeSourceColumnsAsBlobMetadata { get; init; } = false;
    public int MaxBlobMetadataValueLength { get; init; } = 1024;
}
