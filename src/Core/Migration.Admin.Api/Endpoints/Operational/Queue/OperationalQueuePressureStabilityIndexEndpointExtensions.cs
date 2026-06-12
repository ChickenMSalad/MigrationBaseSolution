namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure stability index endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureStabilityIndexEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureStabilityIndexApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/stability-index",
                (string? horizon) =>
                {
                    var bands = BuildStabilityBands();
                    var selectedHorizon = string.IsNullOrWhiteSpace(horizon) ? "current-run" : horizon.Trim();

                    return Results.Ok(new
                    {
                        stabilityIndex = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/stability-index",
                            filters = new
                            {
                                horizon = selectedHorizon
                            },
                            scoreModel = new
                            {
                                minimumScore = 0,
                                maximumScore = 100,
                                higherIsMoreStable = true,
                                source = "operator-facing synthesized guide derived from queue-pressure dashboard, trend, action-plan, recovery, and review endpoints"
                            },
                            stabilityBands = bands,
                            operatorUse = new[]
                            {
                                "Use the dashboard endpoint for current pressure context before interpreting stability.",
                                "Use the trend endpoint to confirm whether pressure is improving or deteriorating.",
                                "Use the recovery workflow and post-recovery review endpoints when stability remains degraded after mitigation."
                            },
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/trend",
                                "/api/operational/queue-pressure/action-plan",
                                "/api/operational/queue-pressure/recovery-workflow",
                                "/api/operational/queue-pressure/post-recovery-review"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureStabilityIndex")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure stability index guide.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/stability-index/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/stability-index",
                        bandCount = BuildStabilityBands().Length
                    }
                }))
            .WithName("GetOperationalQueuePressureStabilityIndexReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure stability index readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressureStabilityBand[] BuildStabilityBands()
    {
        return new[]
        {
            new QueuePressureStabilityBand(
                Name: "Stable",
                ScoreRange: "80-100",
                OperatorMeaning: "Queue pressure is within expected bounds and no immediate intervention is indicated.",
                SuggestedEndpoint: "/api/operational/queue-pressure/dashboard"),
            new QueuePressureStabilityBand(
                Name: "Watch",
                ScoreRange: "60-79",
                OperatorMeaning: "Pressure indicators should be monitored before increasing migration volume.",
                SuggestedEndpoint: "/api/operational/queue-pressure/trend"),
            new QueuePressureStabilityBand(
                Name: "Degraded",
                ScoreRange: "40-59",
                OperatorMeaning: "Operator action may be needed to prevent backlog or dispatcher strain from worsening.",
                SuggestedEndpoint: "/api/operational/queue-pressure/action-plan"),
            new QueuePressureStabilityBand(
                Name: "Unstable",
                ScoreRange: "0-39",
                OperatorMeaning: "Follow the recovery workflow and capture post-recovery evidence before resuming normal activity.",
                SuggestedEndpoint: "/api/operational/queue-pressure/recovery-workflow")
        };
    }

    private sealed record QueuePressureStabilityBand(
        string Name,
        string ScoreRange,
        string OperatorMeaning,
        string SuggestedEndpoint);
}


