namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Policy boundary for deciding when repeated failures become poison work rather than ordinary retryable work.
/// </summary>
public sealed class AzureWorkerPoisonWorkPolicy
{
    public string PolicyName { get; init; } = "default";
    public int MaxConsecutiveFailures { get; init; } = 5;
    public int MaxTotalAttempts { get; init; } = 10;
    public TimeSpan MinimumRetryWindow { get; init; } = TimeSpan.FromMinutes(15);
    public AzureWorkerPoisonWorkAction DefaultPoisonAction { get; init; } = AzureWorkerPoisonWorkAction.RequireOperatorReview;
    public bool QuarantineBeforeDeadLetter { get; init; } = true;
    public bool EmitOperationalEvent { get; init; } = true;
}
