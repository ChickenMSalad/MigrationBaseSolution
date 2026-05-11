namespace Migration.Connectors.Sources.SharePoint.Configuration;

public sealed class SharePointSourceOptions
{
    public string Mode { get; init; } = "Rclone"; // Rclone | Graph

    public GraphOptions Graph { get; init; } = new();
    public RcloneOptions Rclone { get; init; } = new();
    public ManifestOptions Manifest { get; init; } = new();

    public List<string> PathFields { get; init; } = new()
    {
        "SourcePath", "sourcePath", "source_path", "Path", "path",
        "sharepoint_path", "SharePointPath", "FilePath", "filePath", "file_path"
    };

    public List<string> IdFields { get; init; } = new()
    {
        "SourceAssetId", "sourceAssetId", "source_asset_id", "DriveItemId", "driveItemId", "id", "Id"
    };
}

public sealed class GraphOptions
{
    public string? TenantId { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? SiteHostname { get; init; }
    public string? SitePath { get; init; }
    public string? SiteId { get; init; }
    public string? DriveId { get; init; }
    public string DriveName { get; init; } = "Documents";
    public string RootPath { get; init; } = "";
    public bool IncludeGraphMetadata { get; init; } = true;
}

public sealed class RcloneOptions
{
    public string ExecutablePath { get; init; } = "rclone";
    public string? ConfigPath { get; init; }
    public string RemoteName { get; init; } = "sharepoint";
    public string RootPath { get; init; } = "";
    public string? StagingDirectory { get; init; }
    public bool CopyToLocalStaging { get; init; } = true;
    public bool ReuseExistingStagedFile { get; init; } = true;
    public int ProcessTimeoutSeconds { get; init; } = 0;
}

public sealed class ManifestOptions
{
    public bool IncludeFolderMetadata { get; init; } = true;
    public bool IncludeFileNameMetadata { get; init; } = true;
    public bool IncludeGraphMetadata { get; init; } = false;
    public string OutputPath { get; init; } = "sharepoint-manifest.csv";
}
