namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Classification metadata for work that can no longer safely continue through normal retry paths.
/// </summary>
public sealed class AzureWorkerPoisonWorkClassification
{
    public string ClassificationCode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public AzureWorkerPoisonWorkAction DefaultAction { get; init; } = AzureWorkerPoisonWorkAction.RequireOperatorReview;
    public bool BlocksReplayAdmission { get; init; }
    public bool RequiresManualDisposition { get; init; } = true;
}
