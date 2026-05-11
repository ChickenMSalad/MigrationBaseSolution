using System.Collections.Generic;
using Migration.Shared.Configuration.Hosts.Common;

namespace Migration.Shared.Configuration.Hosts.Bynder;

public sealed class BynderHostOptions
{
    public HostPathOptions Paths { get; set; } = new();
    public BynderFileOptions Files { get; set; } = new();
    public BynderColumnOptions Columns { get; set; } = new();
    public BynderMetadataOptions Metadata { get; set; } = new();
    public BynderBynderBatchOptions Batch { get; set; } = new();
}

public sealed class BynderFileOptions
{
    public string? WebDamExportFile { get; set; }
    public string? BynderImportFile { get; set; }
    public string? MetadataTemplateFile { get; set; }
    public string? ClientMetadataTemplateFile { get; set; }
    public string? MetadataPropertiesFile { get; set; }
    public string? ValidationReportFile { get; set; }
    public string? DuplicateReportFile { get; set; }
    public string? FailedRowsFile { get; set; }
    public string? LogFilename { get; set; }
    public string? SuccessRetryFilename { get; set; }
    public string? BlankMetadataTemplate { get; set; }
    public string? MetadataFilename { get; set; }
}

public sealed class BynderColumnOptions
{
    public List<string> IgnoreColumns { get; set; } = new();
    public List<string> ValidationColumns { get; set; } = new();
    public List<string> IgnoreValidationColumns { get; set; } = new();
    public List<string> RequiredColumns { get; set; } = new();
    public List<string> IdentityColumns { get; set; } = new();
    public List<string> UploadColumns { get; set; } = new();
}

public sealed class BynderMetadataOptions
{
    public Dictionary<string, string> ColumnMappings { get; set; } = new();
    public List<string> MultiValueColumns { get; set; } = new();
    public List<string> DateColumns { get; set; } = new();
    public List<string> TaxonomyColumns { get; set; } = new();
}

public sealed class BynderBynderBatchOptions
{
    public int BatchSize { get; set; } = 100;
    public int KnownAssetPages { get; set; } = 100;
    public long MaxBytes { get; set; } = 800L * 1024 * 1024;
    public bool RetryFailuresOnly { get; set; }
    public bool DryRun { get; set; }
}
