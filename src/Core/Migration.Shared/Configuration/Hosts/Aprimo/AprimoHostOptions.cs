using Migration.Shared.Configuration.Hosts.Common;

namespace Migration.Shared.Configuration.Hosts.Aprimo;

public sealed class AprimoHostOptions
{
    public HostPathOptions Paths { get; set; } = new();
    public AprimoFileOptions Files { get; set; } = new();
    public HostRuntimeOptions Runtime { get; set; } = new();
    public AprimoToolOptions Tools { get; set; } = new();
}

public sealed class AprimoFileOptions
{
    public string? StagedAzureAssetFile { get; set; }
    public string? AprimoImportFile { get; set; }
    public string? FailedRowsFile { get; set; }
    public string? RestampFile { get; set; }
    public string? SuccessRetryFilename { get; set; }
    public string? LogFilename { get; set; }
}

/// <summary>
/// Paths to external tools invoked by the Aprimo migration service (e.g. Blender for GLB thumbnail rendering).
/// </summary>
public sealed class AprimoToolOptions
{
    /// <summary>Absolute path to the Blender executable.</summary>
    public string? BlenderExecutablePath { get; set; }

    /// <summary>Absolute path to the Python script used to render GLB thumbnails via Blender.</summary>
    public string? BlenderThumbnailScriptPath { get; set; }
}
