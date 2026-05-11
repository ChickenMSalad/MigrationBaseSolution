namespace Migration.Connectors.Sources.SharePoint.ManifestBuilder;

public sealed record SharePointRcloneManifestItem(
    string RelativePath,
    string FileName,
    string FileNameWithoutExtension,
    string FileExtension,
    string FolderPath,
    int FolderDepth,
    IReadOnlyList<string> FolderSegments);
