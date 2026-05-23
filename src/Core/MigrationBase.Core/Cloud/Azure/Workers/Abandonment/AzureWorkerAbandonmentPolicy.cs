namespace MigrationBase.Core.Cloud.Azure.Workers.Abandonment;

/// <summary>
/// Describes how a host role should treat abandoned work items.
/// </summary>
public sealed class AzureWorkerAbandonmentPolicy
{
    public string PolicyName { get; init; } = string.Empty;

    public string HostRole { get; init; } = string.Empty;

    public bool RequeueOnGracefulShutdown { get; init; } = true;

    public bool RequeueOnLeaseLoss { get; init; } = true;

    public bool RequeueOnHeartbeatStale { get; init; } = true;

    public bool MarkPoisonWhenRetryBudgetExceeded { get; init; } = true;

    public int MaxAbandonmentsBeforePoison { get; init; } = 5;

    public TimeSpan MinimumRequeueDelay { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan MaximumRequeueDelay { get; init; } = TimeSpan.FromMinutes(15);

    public string EvidenceRequirement { get; init; } = "abandonment-reason-and-worker-snapshot";
}
