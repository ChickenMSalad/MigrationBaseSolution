using Migration.Shared.Configuration.Hosts.Common;

namespace Migration.Shared.Configuration.Hosts.Sitecore;

public sealed class SitecoreHostOptions
{
    public HostPathOptions Paths { get; set; } = new();
    public SitecoreFileOptions Files { get; set; } = new();
    public HostBatchOptions Batch { get; set; } = new();
    public HostRuntimeOptions Runtime { get; set; } = new();
}

public sealed class SitecoreFileOptions
{
    public string? BatchBlobListingFile { get; set; }
    public string? NodeAssetsOutputFile { get; set; }
    public string? NodeFlatRowsOutputFile { get; set; }
    public string? LastModifiedAssetIdsFile { get; set; }
    public string? MigrationLogFile { get; set; }
}
