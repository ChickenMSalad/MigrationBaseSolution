namespace Migration.Application.Configuration.Workflows;

public sealed class WebDamToBynderWorkflowOptions
{
    public string? StagingDirectory { get; set; }
    public string? MappingFile { get; set; }
    public string? MetadataTemplateFile { get; set; }
}
