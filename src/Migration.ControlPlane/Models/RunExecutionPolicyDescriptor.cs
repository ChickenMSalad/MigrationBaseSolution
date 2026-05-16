namespace Migration.ControlPlane.Models;

/// <summary>
/// Cloud-facing execution policy for a migration run.
/// This does not change worker behavior yet; it gives the API/UI/worker a stable
/// contract for idempotency, leasing, retry eligibility, and poison handling.
/// </summary>
public sealed record RunExecutionPolicyDescriptor(
    string RunId,
    string JobName,
    string Status,
    string LifecycleStage,
    bool IsTerminal,
    string IdempotencyKey,
    string LeaseResource,
    int LeaseDurationSeconds,
    int HeartbeatIntervalSeconds,
    int MaxAttempts,
    bool CanAcquireLease,
    bool CanRetry,
    bool CanRetryFailures,
    bool CanResume,
    bool ShouldDeadLetterOnMaxAttempts,
    string PoisonHandlingMode,
    IReadOnlyList<string> RecommendedWorkerActions,
    DateTimeOffset UpdatedUtc,
    DateTimeOffset? CompletedUtc);

public static class RunPoisonHandlingModes
{
    public const string MarkFailed = "markFailed";
    public const string DeadLetter = "deadLetter";
    public const string ManualReview = "manualReview";
}
