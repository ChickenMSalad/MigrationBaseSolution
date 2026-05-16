using System.Security.Cryptography;
using System.Text;
using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

/// <summary>
/// Builds deterministic execution-policy metadata for queue workers and cloud
/// orchestration. The policy is derived from current run state and does not
/// mutate the run.
/// </summary>
public static class RunExecutionPolicyBuilder
{
    private const int DefaultLeaseDurationSeconds = 300;
    private const int DefaultHeartbeatIntervalSeconds = 60;
    private const int DefaultMaxAttempts = 3;

    public static RunExecutionPolicyDescriptor Build(MigrationRunControlRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var lifecycle = RunLifecycleClassifier.Describe(run);
        var settings = run.Job.Settings;

        var maxAttempts = ReadInt(settings, "MaxAttempts", DefaultMaxAttempts);
        var leaseDurationSeconds = ReadInt(settings, "LeaseDurationSeconds", DefaultLeaseDurationSeconds);
        var heartbeatIntervalSeconds = ReadInt(settings, "HeartbeatIntervalSeconds", DefaultHeartbeatIntervalSeconds);
        var poisonMode = ReadString(settings, "PoisonHandlingMode", RunPoisonHandlingModes.DeadLetter);

        var canAcquireLease = !lifecycle.IsTerminal && lifecycle.CanCancel;
        var shouldDeadLetterOnMaxAttempts = !string.Equals(
            poisonMode,
            RunPoisonHandlingModes.MarkFailed,
            StringComparison.OrdinalIgnoreCase);

        return new RunExecutionPolicyDescriptor(
            RunId: run.RunId,
            JobName: run.JobName,
            Status: run.Status,
            LifecycleStage: lifecycle.LifecycleStage,
            IsTerminal: lifecycle.IsTerminal,
            IdempotencyKey: CreateIdempotencyKey(run),
            LeaseResource: CreateLeaseResource(run),
            LeaseDurationSeconds: leaseDurationSeconds,
            HeartbeatIntervalSeconds: heartbeatIntervalSeconds,
            MaxAttempts: maxAttempts,
            CanAcquireLease: canAcquireLease,
            CanRetry: lifecycle.CanRetry,
            CanRetryFailures: lifecycle.CanRetryFailures,
            CanResume: lifecycle.CanResume,
            ShouldDeadLetterOnMaxAttempts: shouldDeadLetterOnMaxAttempts,
            PoisonHandlingMode: poisonMode,
            RecommendedWorkerActions: GetRecommendedWorkerActions(lifecycle, canAcquireLease),
            UpdatedUtc: run.UpdatedUtc,
            CompletedUtc: run.CompletedUtc);
    }

    private static IReadOnlyList<string> GetRecommendedWorkerActions(
        RunLifecycleDescriptor lifecycle,
        bool canAcquireLease)
    {
        if (lifecycle.IsTerminal)
        {
            return new[]
            {
                "Do not acquire a new worker lease.",
                "Do not enqueue additional work items.",
                "Expose retry actions through control-plane APIs only."
            };
        }

        if (canAcquireLease)
        {
            return new[]
            {
                "Acquire a lease using the lease resource before processing.",
                "Refresh heartbeat while processing.",
                "Persist work-item checkpoints after each item.",
                "Use idempotency key before creating duplicate downstream work."
            };
        }

        return new[]
        {
            "Inspect run state before processing.",
            "Avoid duplicate execution until lifecycle state is known."
        };
    }

    private static string CreateLeaseResource(MigrationRunControlRecord run) =>
        $"migration-run:{Normalize(run.RunId)}";

    private static string CreateIdempotencyKey(MigrationRunControlRecord run)
    {
        var input = string.Join(
            "|",
            Normalize(run.RunId),
            Normalize(run.JobName),
            Normalize(run.Job.SourceType),
            Normalize(run.Job.TargetType),
            Normalize(run.Job.ManifestType),
            Normalize(run.ManifestArtifactId),
            Normalize(run.MappingArtifactId));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static int ReadInt(
        IReadOnlyDictionary<string, string?> settings,
        string key,
        int defaultValue)
    {
        if (!settings.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value) ||
            !int.TryParse(value, out var parsed) ||
            parsed <= 0)
        {
            return defaultValue;
        }

        return parsed;
    }

    private static string ReadString(
        IReadOnlyDictionary<string, string?> settings,
        string key,
        string defaultValue)
    {
        if (!settings.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim();
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim().ToLowerInvariant();
}
