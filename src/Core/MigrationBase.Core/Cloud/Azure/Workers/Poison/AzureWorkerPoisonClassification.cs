namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Classifies why a work item entered a poison or abandonment path.
/// </summary>
public sealed class AzureWorkerPoisonClassification
{
    public required string Code { get; init; }
    public required string Reason { get; init; }
    public AzureWorkerPoisonDisposition Disposition { get; init; } = AzureWorkerPoisonDisposition.Quarantine;
    public bool IsOperatorActionRequired { get; init; }
    public bool IsReplayEligible { get; init; }
    public TimeSpan? SuggestedDelay { get; init; }
}
