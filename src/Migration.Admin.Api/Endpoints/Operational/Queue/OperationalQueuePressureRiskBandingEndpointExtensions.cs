namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure risk banding endpoints.
/// This endpoint is intentionally read-only and compile-safe: it exposes deterministic operator risk band guidance.
/// </summary>
public static class OperationalQueuePressureRiskBandingEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureRiskBandingApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/risk-banding",
                (string? pressureLevel, string? queueTrend, string? dispatcherState) =>
                {
                    var selectedPressureLevel = Normalize(pressureLevel, "Elevated");
                    var selectedQueueTrend = Normalize(queueTrend, "Stable");
                    var selectedDispatcherState = Normalize(dispatcherState, "Nominal");
                    var score = CalculateRiskScore(selectedPressureLevel, selectedQueueTrend, selectedDispatcherState);
                    var band = ResolveRiskBand(score);
                    var bands = BuildBands();

                    return Results.Ok(new
                    {
                        riskBanding = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/risk-banding",
                            filters = new
                            {
                                pressureLevel = selectedPressureLevel,
                                queueTrend = selectedQueueTrend,
                                dispatcherState = selectedDispatcherState
                            },
                            isReadOnly = true,
                            purpose = "Read-only operator risk banding for queue pressure review and escalation consistency.",
                            summary = new
                            {
                                riskScore = score,
                                riskBand = band.Name,
                                riskLevel = band.Level,
                                recommendedPosture = band.RecommendedPosture,
                                requiresEscalation = band.RequiresEscalation,
                                requiresNamedOwner = score >= 70
                            },
                            bands,
                            contributingSignals = new[]
                            {
                                new QueuePressureRiskSignal("Pressure Level", selectedPressureLevel, ScorePressure(selectedPressureLevel)),
                                new QueuePressureRiskSignal("Queue Trend", selectedQueueTrend, ScoreTrend(selectedQueueTrend)),
                                new QueuePressureRiskSignal("Dispatcher State", selectedDispatcherState, ScoreDispatcher(selectedDispatcherState))
                            },
                            guardrails = new[]
                            {
                                "Risk banding is guidance-only and does not execute mitigation.",
                                "Use safety-review before applying any throughput or throttle change.",
                                "Escalate Critical or Severe bands before attempting manual recovery."
                            },
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/decision-matrix",
                                "/api/operational/queue-pressure/operator-advisory",
                                "/api/operational/queue-pressure/safety-review",
                                "/api/operational/queue-pressure/escalation-guide"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureRiskBanding")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure risk banding guidance.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/risk-banding/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/risk-banding",
                        mode = "GuidanceOnly",
                        isReadOnly = true,
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" },
                        supportedQueueTrends = new[] { "Improving", "Stable", "Worsening", "Surging" },
                        supportedDispatcherStates = new[] { "Nominal", "Constrained", "Degraded", "Blocked" }
                    }
                }))
            .WithName("GetOperationalQueuePressureRiskBandingReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure risk banding endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static int CalculateRiskScore(string pressureLevel, string queueTrend, string dispatcherState)
    {
        return Math.Min(100, ScorePressure(pressureLevel) + ScoreTrend(queueTrend) + ScoreDispatcher(dispatcherState));
    }

    private static int ScorePressure(string pressureLevel)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        {
            return 55;
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
        {
            return 40;
        }

        if (pressureLevel.Equals("Elevated", StringComparison.OrdinalIgnoreCase))
        {
            return 25;
        }

        return 10;
    }

    private static int ScoreTrend(string queueTrend)
    {
        if (queueTrend.Equals("Surging", StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        if (queueTrend.Equals("Worsening", StringComparison.OrdinalIgnoreCase))
        {
            return 22;
        }

        if (queueTrend.Equals("Stable", StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        return 4;
    }

    private static int ScoreDispatcher(string dispatcherState)
    {
        if (dispatcherState.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        if (dispatcherState.Equals("Degraded", StringComparison.OrdinalIgnoreCase))
        {
            return 22;
        }

        if (dispatcherState.Equals("Constrained", StringComparison.OrdinalIgnoreCase))
        {
            return 15;
        }

        return 5;
    }

    private static QueuePressureRiskBand ResolveRiskBand(int score)
    {
        if (score >= 85)
        {
            return new QueuePressureRiskBand("Critical", "Severe", 85, 100, "Escalate before change; assign incident owner.", true);
        }

        if (score >= 65)
        {
            return new QueuePressureRiskBand("High", "High", 65, 84, "Use lead-reviewed mitigation and frequent review cadence.", true);
        }

        if (score >= 40)
        {
            return new QueuePressureRiskBand("Elevated", "Moderate", 40, 64, "Monitor actively; prepare conservative mitigation options.", false);
        }

        return new QueuePressureRiskBand("Normal", "Low", 0, 39, "Monitor only; no mitigation recommended.", false);
    }

    private static QueuePressureRiskBand[] BuildBands()
    {
        return new[]
        {
            new QueuePressureRiskBand("Normal", "Low", 0, 39, "Monitor only; no mitigation recommended.", false),
            new QueuePressureRiskBand("Elevated", "Moderate", 40, 64, "Monitor actively; prepare conservative mitigation options.", false),
            new QueuePressureRiskBand("High", "High", 65, 84, "Use lead-reviewed mitigation and frequent review cadence.", true),
            new QueuePressureRiskBand("Critical", "Severe", 85, 100, "Escalate before change; assign incident owner.", true)
        };
    }

    private sealed record QueuePressureRiskSignal(string Name, string ObservedValue, int ScoreContribution);

    private sealed record QueuePressureRiskBand(
        string Name,
        string Level,
        int MinimumScore,
        int MaximumScore,
        string RecommendedPosture,
        bool RequiresEscalation);
}
