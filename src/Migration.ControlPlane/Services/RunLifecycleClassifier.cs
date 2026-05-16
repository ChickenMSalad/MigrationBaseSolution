using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

/// <summary>
/// Normalizes existing AdminRunStatuses into cloud-facing lifecycle semantics.
/// This does not change storage or worker behavior; it gives API/UI/worker code
/// one place to ask what a run status means.
/// </summary>
public static class RunLifecycleClassifier
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public static RunLifecycleDescriptor Describe(MigrationRunControlRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var stage = GetLifecycleStage(run.Status);
        var isTerminal = IsTerminalStatus(run.Status);
        var isPreflight = IsPreflightRun(run);

        return new RunLifecycleDescriptor(
            RunId: run.RunId,
            Status: run.Status,
            LifecycleStage: stage,
            IsPreflight: isPreflight,
            IsTerminal: isTerminal,
            CanCancel: CanCancel(run.Status),
            CanRetry: CanRetry(run.Status),
            CanRetryFailures: CanRetryFailures(run.Status),
            CanResume: CanResume(run.Status),
            CanViewWorkItems: CanViewWorkItems(run.Status),
            Message: run.Message,
            UpdatedUtc: run.UpdatedUtc,
            CompletedUtc: run.CompletedUtc);
    }

    public static string GetLifecycleStage(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return RunLifecycleStages.Unknown;
        }

        if (Comparer.Equals(status, AdminRunStatuses.Completed))
        {
            return RunLifecycleStages.Completed;
        }

        if (Comparer.Equals(status, AdminRunStatuses.Failed))
        {
            return RunLifecycleStages.Failed;
        }

        if (Comparer.Equals(status, AdminRunStatuses.Canceled))
        {
            return RunLifecycleStages.Canceled;
        }

        if (Contains(status, "running") ||
            Contains(status, "processing") ||
            Contains(status, "executing") ||
            Contains(status, "started"))
        {
            return RunLifecycleStages.Running;
        }

        if (Contains(status, "queued") ||
            Contains(status, "pending") ||
            Contains(status, "created") ||
            Contains(status, "accepted"))
        {
            return RunLifecycleStages.Queued;
        }

        return RunLifecycleStages.Unknown;
    }

    public static bool IsTerminalStatus(string? status)
    {
        var stage = GetLifecycleStage(status);
        return stage is RunLifecycleStages.Completed or RunLifecycleStages.Failed or RunLifecycleStages.Canceled;
    }

    public static bool CanCancel(string? status)
    {
        var stage = GetLifecycleStage(status);
        return stage is RunLifecycleStages.Queued or RunLifecycleStages.Running or RunLifecycleStages.Unknown;
    }

    public static bool CanRetry(string? status)
    {
        var stage = GetLifecycleStage(status);
        return stage is RunLifecycleStages.Failed or RunLifecycleStages.Canceled;
    }

    public static bool CanRetryFailures(string? status)
    {
        var stage = GetLifecycleStage(status);
        return stage is RunLifecycleStages.Completed or RunLifecycleStages.Failed or RunLifecycleStages.Canceled;
    }

    public static bool CanResume(string? status)
    {
        var stage = GetLifecycleStage(status);
        return stage is RunLifecycleStages.Failed or RunLifecycleStages.Canceled or RunLifecycleStages.Unknown;
    }

    public static bool CanViewWorkItems(string? status)
    {
        var stage = GetLifecycleStage(status);
        return stage is not RunLifecycleStages.Unknown;
    }

    public static bool IsPreflightRun(MigrationRunControlRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (Comparer.Equals(run.Status, AdminRunStatuses.PreflightQueued))
        {
            return true;
        }

        if (run.Job.Settings.TryGetValue("PreflightOnly", out var value) &&
            bool.TryParse(value, out var parsed) &&
            parsed)
        {
            return true;
        }

        return run.RunId.StartsWith("preflight-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string value, string fragment) =>
        value.Contains(fragment, StringComparison.OrdinalIgnoreCase);
}
