namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionItemRequest
{
    public required AzureManifestExecutionContext Context { get; init; }

    public required AzureManifestExecutionItem Item { get; init; }

    public int AttemptNumber { get; init; } = 1;

    public bool IsDryRun => Context.Plan.Scope.Mode == AzureManifestExecutionMode.DryRun;

    public bool IsValidateOnly => Context.Plan.Scope.Mode == AzureManifestExecutionMode.ValidateOnly;
}
