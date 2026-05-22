using System.Collections.Generic;
using Migration.Shared.Configuration.Hosts.Common;

namespace Migration.Shared.Configuration.Hosts.Aem;

public sealed class AemHostOptions
{
    public HostPathOptions Paths { get; set; } = new();
    public AemFileOptions Files { get; set; } = new();
    public HostRuntimeOptions Runtime { get; set; } = new();
    public AemColumnOptions Columns { get; set; } = new();
}

public sealed class AemFileOptions
{
    public string? AssetsFile { get; set; }
    public string? MappingHelperObjectsFile { get; set; }
    public string? DuplicateAssetsFile { get; set; }
    public string? RelatedDataFile { get; set; }
    public string? ValidationFile { get; set; }
    public string? SuccessRetryFilename { get; set; }
    public string? LogFilename { get; set; }
}

public sealed class AemColumnOptions
{
    public List<string> IgnoreColumns { get; set; } = new();
    public List<string> ValidationColumns { get; set; } = new();
    public List<string> RequiredColumns { get; set; } = new();
}
