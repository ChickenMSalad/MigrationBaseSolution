namespace Migration.Connectors.Sources.LocalStorage;

public sealed class LocalStorageSourceOptions
{
    public string? RootDirectory { get; init; }

    public bool RequireExistingFile { get; init; } = true;

    public List<string> PathFields { get; init; } = new()
    {
        "SourcePath",
        "sourcePath",
        "FilePath",
        "filePath",
        "Path",
        "path",
        "LocalPath",
        "localPath",
        "SourceUri",
        "sourceUri"
    };

    public List<string> FileNameFields { get; init; } = new()
    {
        "FileName",
        "fileName",
        "filename",
        "OriginalFileName",
        "originalFileName",
        "Name",
        "name"
    };

    public List<string> IdFields { get; init; } = new()
    {
        "SourceAssetId",
        "sourceAssetId",
        "AssetId",
        "assetId",
        "id",
        "Id"
    };
}
