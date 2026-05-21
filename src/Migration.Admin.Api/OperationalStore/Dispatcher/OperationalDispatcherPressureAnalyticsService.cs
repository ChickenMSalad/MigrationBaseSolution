
namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherPressureAnalyticsService
    : IOperationalDispatcherPressureAnalyticsService
{
    private readonly IOperationalGlobalQueueDepthAnalyticsService _queueDepthService;

    public OperationalDispatcherPressureAnalyticsService(
        IOperationalGlobalQueueDepthAnalyticsService queueDepthService)
    {
        _queueDepthService = queueDepthService;
    }

    public async Task<OperationalDispatcherPressureAnalyticsResponse> GetAnalyticsAsync(
        int metricsSampleLimit = 100,
        CancellationToken cancellationToken = default)
    {
        var queueDepth = await _queueDepthService.GetAnalyticsAsync(
            cancellationToken);

        var pressureScore = CalculatePressureScore(queueDepth);

        return new OperationalDispatcherPressureAnalyticsResponse
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            QueueDepth = queueDepth,
            PressureScore = pressureScore,
            PressureLevel = ToPressureLevel(pressureScore),
            PressureReason = BuildPressureReason(queueDepth, pressureScore),
            OutstandingWorkItemCount = queueDepth.OutstandingWorkItemCount,
            LockedWorkItemCount = queueDepth.LockedWorkItemCount,
            FailedWorkItemCount = queueDepth.FailedWorkItemCount,
            Signals = BuildSignals(queueDepth, pressureScore)
        };
    }

    private static int CalculatePressureScore(
        OperationalGlobalQueueDepthAnalyticsResponse queueDepth)
    {
        var score = 0;

        score += Math.Min(queueDepth.OutstandingWorkItemCount * 3, 45);
        score += Math.Min(queueDepth.LockedWorkItemCount * 2, 25);
        score += Math.Min(queueDepth.FailedWorkItemCount * 8, 30);

        return Math.Clamp(score, 0, 100);
    }

    private static string BuildPressureReason(
        OperationalGlobalQueueDepthAnalyticsResponse queueDepth,
        int score)
    {
        if (score == 0)
        {
            return "No dispatcher pressure is currently visible from queue-depth signals.";
        }

        if (queueDepth.FailedWorkItemCount > 0)
        {
            return "Failed work items are contributing to dispatcher pressure.";
        }

        if (queueDepth.OutstandingWorkItemCount > 0)
        {
            return "Outstanding work items are contributing to dispatcher pressure.";
        }

        if (queueDepth.LockedWorkItemCount > 0)
        {
            return "Locked work items are contributing to dispatcher pressure.";
        }

        return "Dispatcher pressure is present due to queue-depth signals.";
    }

    private static IReadOnlyCollection<string> BuildSignals(
        OperationalGlobalQueueDepthAnalyticsResponse queueDepth,
        int score)
    {
        var signals = new List<string>
        {
            $"Dispatcher pressure score is {score}.",
            $"Dispatcher pressure level is {ToPressureLevel(score)}.",
            $"Outstanding work item count is {queueDepth.OutstandingWorkItemCount}.",
            $"Locked work item count is {queueDepth.LockedWorkItemCount}.",
            $"Failed work item count is {queueDepth.FailedWorkItemCount}.",
            $"Queue pressure score is {queueDepth.QueuePressureScore}.",
            $"Queue pressure level is {queueDepth.QueuePressureLevel}."
        };

        if (queueDepth.OutstandingWorkItemCount == 0)
        {
            signals.Add("No outstanding queue backlog is currently visible.");
        }

        if (queueDepth.LockedWorkItemCount == 0)
        {
            signals.Add("No locked work item pressure is currently visible.");
        }

        if (queueDepth.FailedWorkItemCount == 0)
        {
            signals.Add("No failed work-item pressure is currently visible.");
        }

        return signals;
    }

    private static string ToPressureLevel(int score)
    {
        if (score >= 75)
        {
            return "Critical";
        }

        if (score >= 50)
        {
            return "High";
        }

        if (score >= 25)
        {
            return "Elevated";
        }

        return "Normal";
    }
}
