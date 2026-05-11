namespace Migration.Connectors.Targets.LocalStorage;

public sealed class LocalStorageTargetOptions
{
    public string? RootDirectory { get; init; }

    public string? BasePath { get; init; }

    public bool CreateDirectoryIfMissing { get; init; } = true;

    public bool Overwrite { get; init; } = false;

    public bool PreserveSourceFolderPath { get; init; } = true;

    public string? SourceFolderPathField { get; init; }

    public bool PrefixFileNameWithUniqueId { get; init; } = true;

    public string UniqueIdField { get; init; } = "SourceAssetId";

    public bool WriteMetadataSidecar { get; init; } = true;

    public LocalStorageMetadataSidecarMode MetadataSidecarMode { get; init; } = LocalStorageMetadataSidecarMode.Both;

    public List<string> MetadataIncludeColumns { get; init; } = new();

    public List<string> MetadataExcludeColumns { get; init; } = new()
    {
        "SourcePath",
        "sourcePath",
        "FilePath",
        "filePath",
        "Path",
        "path",
        "DownloadUrl",
        "downloadUrl",
        "Url",
        "url",
        "SourceUri",
        "sourceUri"
    };

    public bool IncludeSystemMetadata { get; init; } = true;
}
