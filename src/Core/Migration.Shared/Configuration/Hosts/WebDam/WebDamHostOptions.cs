using Migration.Shared.Configuration.Hosts.Common;

namespace Migration.Shared.Configuration.Hosts.WebDam;

public sealed class WebDamHostOptions
{
    public HostPathOptions Paths { get; set; } = new();
    public WebDamFileOptions Files { get; set; } = new();
    public HostBatchOptions Batch { get; set; } = new();
}

public sealed class WebDamFileOptions
{
    public string? ExportFile { get; set; }
    public string? FolderExportFile { get; set; }
    public string? SchemaExportFile { get; set; }
}
