namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationOptions
{
    public const string SectionName = "AzureRuntime:EndToEndValidation";

    public bool Enabled { get; set; } = true;

    public bool RequireQueueDispatchValidation { get; set; } = true;

    public bool RequireManifestExecutionValidation { get; set; } = true;

    public bool RequireConnectorExecutionValidation { get; set; } = true;

    public bool TreatWarningsAsFailures { get; set; }
}
