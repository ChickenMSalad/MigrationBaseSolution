namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionValidationOptions
{
    public const string SectionName = "AzureRuntime:ConnectorExecutionValidation";

    public bool Enabled { get; set; } = true;

    public bool RequireRunId { get; set; } = true;

    public bool RequireManifestId { get; set; } = true;

    public bool RequireItemId { get; set; } = true;

    public bool RequireSourceIdentifier { get; set; } = true;

    public bool RequireTargetIdentifierForWrite { get; set; }
}
