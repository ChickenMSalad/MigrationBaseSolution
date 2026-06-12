namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure capacity forecast endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureCapacityForecastEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureCapacityForecastApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/capacity-forecast",
                (int? horizonHours) =>
                {
                    var selectedHorizonHours = Math.Clamp(horizonHours.GetValueOrDefault(24), 1, 168);
                    var forecastBands = BuildForecastBands(selectedHorizonHours);

                    return Results.Ok(new
                    {
                        capacityForecast = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/capacity-forecast",
                            filters = new
                            {
                                horizonHours = selectedHorizonHours
                            },
                            purpose = "Operator-facing forecast guidance for planning migration throughput while queue pressure is being monitored.",
                            forecastBands,
                            interpretation = new[]
                            {
                                "Use capacity guardrails before increasing migration volume.",
                                "Use trend and stability endpoints to confirm whether pressure is improving or worsening.",
                                "Treat this endpoint as operational planning guidance, not as a durable scheduling engine."
                            },
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/trend",
                                "/api/operational/queue-pressure/stability-index",
                                "/api/operational/queue-pressure/capacity-guardrails",
                                "/api/operational/queue-pressure/action-plan"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureCapacityForecast")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure capacity forecast guidance.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/capacity-forecast/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/capacity-forecast",
                        forecastBandCount = BuildForecastBands(24).Length
                    }
                }))
            .WithName("GetOperationalQueuePressureCapacityForecastReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure capacity forecast readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressureCapacityForecastBand[] BuildForecastBands(int horizonHours)
    {
        return new[]
        {
            new QueuePressureCapacityForecastBand(
                Window: $"0-{Math.Min(4, horizonHours)} hours",
                PlanningPosture: "Stabilize",
                Guidance: "Avoid increasing throughput until current queue pressure and dispatcher pressure are confirmed stable.",
                SuggestedEndpoint: "/api/operational/queue-pressure/dashboard"),
            new QueuePressureCapacityForecastBand(
                Window: $"{Math.Min(4, horizonHours)}-{Math.Min(24, horizonHours)} hours",
                PlanningPosture: "Validate trend",
                Guidance: "Check trend and stability index before allowing additional migration volume.",
                SuggestedEndpoint: "/api/operational/queue-pressure/trend"),
            new QueuePressureCapacityForecastBand(
                Window: $"24-{horizonHours} hours",
                PlanningPosture: "Plan guarded increase",
                Guidance: "Use capacity guardrails and operator checklist before scheduling larger batches.",
                SuggestedEndpoint: "/api/operational/queue-pressure/capacity-guardrails")
        };
    }

    private sealed record QueuePressureCapacityForecastBand(
        string Window,
        string PlanningPosture,
        string Guidance,
        string SuggestedEndpoint);
}


