namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure recommendation catalog endpoints.
/// This endpoint is intentionally static/metadata-oriented so it remains compile-safe and does not introduce
/// new operational analytics service or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureRecommendationCatalogEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureRecommendationCatalogApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/recommendation-catalog",
                (string? category, string? severity) =>
                {
                    var recommendations = BuildCatalog();

                    if (!string.IsNullOrWhiteSpace(category))
                    {
                        recommendations = recommendations
                            .Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    if (!string.IsNullOrWhiteSpace(severity))
                    {
                        recommendations = recommendations
                            .Where(x => string.Equals(x.Severity, severity, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    return Results.Ok(new
                    {
                        recommendationCatalog = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            filters = new
                            {
                                category,
                                severity
                            },
                            totalRecommendationCount = recommendations.Length,
                            categories = recommendations
                                .Select(x => x.Category)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(x => x)
                                .ToArray(),
                            severities = recommendations
                                .Select(x => x.Severity)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(x => x)
                                .ToArray(),
                            recommendations
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureRecommendationCatalog")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure operator recommendation catalog.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/recommendation-catalog/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        recommendationCount = BuildCatalog().Length,
                        endpoint = "/api/operational/queue-pressure/recommendation-catalog"
                    }
                }))
            .WithName("GetOperationalQueuePressureRecommendationCatalogReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure recommendation catalog readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressureRecommendation[] BuildCatalog()
    {
        return new[]
        {
            new QueuePressureRecommendation(
                Id: "queue-depth-review",
                Category: "Queue",
                Severity: "Info",
                Title: "Review queue depth before scaling workers",
                Recommendation: "Check queue depth and outstanding work item volume before changing migration worker capacity.",
                Rationale: "Queue pressure should be understood before adding dispatcher or worker throughput."),
            new QueuePressureRecommendation(
                Id: "dispatcher-compare",
                Category: "Dispatcher",
                Severity: "Info",
                Title: "Compare dispatcher pressure with queue depth",
                Recommendation: "Use dispatcher pressure together with queue pressure to determine whether the system is building backlog or draining normally.",
                Rationale: "Dispatcher pressure without queue context can produce misleading operational conclusions."),
            new QueuePressureRecommendation(
                Id: "sustained-pressure-check",
                Category: "Trend",
                Severity: "Warning",
                Title: "Confirm whether pressure is transient or sustained",
                Recommendation: "Use the queue pressure trend endpoint before escalating capacity or run-control actions.",
                Rationale: "Sustained pressure requires different action than a short migration burst."),
            new QueuePressureRecommendation(
                Id: "failure-correlation",
                Category: "Failure",
                Severity: "Warning",
                Title: "Correlate queue pressure with failure activity",
                Recommendation: "If queue pressure rises while throughput falls, review recent failures and retry patterns before adding concurrency.",
                Rationale: "Adding concurrency during failure storms can increase retries and operational noise."),
            new QueuePressureRecommendation(
                Id: "operator-readiness",
                Category: "Readiness",
                Severity: "Critical",
                Title: "Verify composed operational endpoints are registered",
                Recommendation: "Confirm dashboard, trend, and action-plan endpoints are visible in endpoint discovery before relying on this catalog during operations.",
                Rationale: "The catalog is most useful when the companion queue pressure views are also available."),
            new QueuePressureRecommendation(
                Id: "no-api-api-route",
                Category: "Validation",
                Severity: "Critical",
                Title: "Reject duplicate api route registration",
                Recommendation: "Endpoint discovery must not expose /api/api/operational/queue-pressure routes.",
                Rationale: "Duplicate api prefixes indicate endpoint registration drift and break expected operator scripts.")
        };
    }

    private sealed record QueuePressureRecommendation(
        string Id,
        string Category,
        string Severity,
        string Title,
        string Recommendation,
        string Rationale);
}
