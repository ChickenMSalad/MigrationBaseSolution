namespace Migration.Application.Configuration.Workflows;

public sealed class SitecoreToBynderWorkflowOptions
{
    public string? StagingDirectory { get; set; }
    public string? MappingFile { get; set; }
    public string? MetadataTemplateFile { get; set; }
    public string? ModifiedAfterUtc { get; set; }
    public bool UseAzureIntermediate { get; set; } = true;
}
