namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionPlanRequest
{
    public required AzureManifestExecutionScope Scope { get; init; }

    public bool IncludeValidationStep { get; init; } = true;

    public bool IncludePreparationStep { get; init; } = true;

    public bool IncludeExecutionStep { get; init; } = true;

    public bool IncludeVerificationStep { get; init; } = true;

    public bool IncludeCompletionStep { get; init; } = true;
}
