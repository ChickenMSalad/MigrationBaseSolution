namespace Migration.Connectors.Sources.SharePoint.ManifestBuilder;

public sealed class SharePointRcloneManifestRow
{
    public required string RowId { get; init; }
    public required string SourceAssetId { get; init; }
    public required string SourcePath { get; init; }
    public required string FileName { get; init; }
    public required string FileNameWithoutExtension { get; init; }
    public required string FileExtension { get; init; }
    public required string FolderPath { get; init; }
    public required string FolderName { get; init; }
    public required int FolderDepth { get; init; }
    public required string TopLevelFolder { get; init; }
    public required string RelativePath { get; init; }
    public long? SizeBytes { get; init; }
    public DateTimeOffset? ModifiedUtc { get; init; }
    public required string SourceSystem { get; init; }
    public required string SourceService { get; init; }
    public required string RcloneRemote { get; init; }
    public string? SharePointSiteUrl { get; init; }
    public string? DocumentLibrary { get; init; }
}
