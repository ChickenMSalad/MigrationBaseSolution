namespace Migration.Application.Configuration.Workflows;

public sealed class AemToAprimoWorkflowOptions
{
    public string? StagingDirectory { get; set; }
    public string? MappingFile { get; set; }
    public string? AssetManifestFile { get; set; }
    public bool UseAzureIntermediate { get; set; } = true;
}
