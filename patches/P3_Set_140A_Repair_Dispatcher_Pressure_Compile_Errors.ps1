$repoRoot = (Resolve-Path ".").Path

$responsePath = Join-Path $repoRoot "src\Migration.Admin.Api\OperationalStore\Dispatcher\OperationalDispatcherPressureAnalyticsResponse.cs"
$servicePath = Join-Path $repoRoot "src\Migration.Admin.Api\OperationalStore\Dispatcher\OperationalDispatcherPressureAnalyticsService.cs"

if (-not (Test-Path $responsePath)) {
    throw "Could not find $responsePath"
}

if (-not (Test-Path $servicePath)) {
    throw "Could not find $servicePath"
}

Copy-Item -Path $responsePath -Destination "$responsePath.140A.bak" -Force
Copy-Item -Path $servicePath -Destination "$servicePath.140A.bak" -Force

@'

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherPressureAnalyticsResponse
{
    public DateTimeOffset GeneratedAt { get; init; }

    public OperationalGlobalQueueDepthAnalyticsResponse QueueDepth { get; init; } = default!;

    public int PressureScore { get; init; }

    public string PressureLevel { get; init; } = string.Empty;

    public string PressureReason { get; init; } = string.Empty;

    public int OutstandingWorkItemCount { get; init; }

    public int LockedWorkItemCount { get; init; }

    public int FailedWorkItemCount { get; init; }

    public IReadOnlyCollection<string> Signals { get; init; } =
        Array.Empty<string>();
}

'@ | Set-Content -Path $responsePath -NoNewline

@'

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

'@ | Set-Content -Path $servicePath -NoNewline

Write-Host "Repaired dispatcher pressure response/service to remove missing dispatcher metrics/readiness type references."
Write-Host ""
Write-Host "Next:"
Write-Host "  dotnet build"
Write-Host "  Restart Admin API"
Write-Host "  ./scripts/operational-dispatcher-pressure-full-smoke-test.ps1 -BaseUrl `"https://localhost:55436`""
