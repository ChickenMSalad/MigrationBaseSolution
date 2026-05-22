using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.SqlMetrics;

namespace Migration.Admin.Api.Endpoints.Operational.Audit;

public static class OperationalAuditTrailEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalAuditTrailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/audit-trail")
            .WithTags("Operational Audit Trail");

        group.MapGet("/summary", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var runtimeEvents = snapshot.QueueDepth > 0 ? 1 : 0;
            var configurationEvents = snapshot.Status is "not-configured" ? 1 : 0;
            var securityEvents = 0;
            var failureEvents = snapshot.FailureCount > 0 ? 1 : 0;
            var totalEvents = runtimeEvents + configurationEvents + securityEvents + failureEvents;

            var response = new OperationalAuditTrailSummaryResponse(
                TotalEvents: totalEvents,
                SecurityEvents: securityEvents,
                RuntimeEvents: runtimeEvents + failureEvents,
                ConfigurationEvents: configurationEvents,
                LastEventUtc: totalEvents > 0 ? DateTimeOffset.UtcNow : null,
                Status: snapshot.Status);

            return Results.Ok(response);
        })
        .WithName("GetOperationalAuditTrailSummary");

        group.MapGet("/recent", async (
            ISqlOperationalMetricsReader metricsReader,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var safeTake = Math.Clamp(take.GetValueOrDefault(50), 1, 250);
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var events = new List<OperationalAuditTrailEventResponse>();

            if (snapshot.Status is "not-configured" or "unhealthy")
            {
                events.Add(new OperationalAuditTrailEventResponse(
                    EventId: "operational-sql-health",
                    OccurredUtc: DateTimeOffset.UtcNow,
                    Category: "infrastructure",
                    Action: "OperationalSqlHealthEvaluated",
                    Actor: "system",
                    ResourceType: "OperationalSql",
                    ResourceId: "default",
                    Outcome: snapshot.Status,
                    Message: snapshot.Message ?? "Operational SQL runtime is not healthy."));
            }

            if (snapshot.QueueDepth > 0)
            {
                events.Add(new OperationalAuditTrailEventResponse(
                    EventId: "operational-queue-depth",
                    OccurredUtc: DateTimeOffset.UtcNow,
                    Category: "runtime",
                    Action: "QueueDepthObserved",
                    Actor: "system",
                    ResourceType: "MigrationWorkItems",
                    ResourceId: "default",
                    Outcome: "observed",
                    Message: $"{snapshot.QueueDepth} operational work item(s) currently exist."));
            }

            if (snapshot.FailureCount > 0)
            {
                events.Add(new OperationalAuditTrailEventResponse(
                    EventId: "operational-failures",
                    OccurredUtc: DateTimeOffset.UtcNow,
                    Category: "runtime",
                    Action: "FailuresObserved",
                    Actor: "system",
                    ResourceType: "MigrationFailures",
                    ResourceId: "default",
                    Outcome: "attention-required",
                    Message: $"{snapshot.FailureCount} migration failure record(s) currently exist."));
            }

            var response = new OperationalAuditTrailRecentResponse(
                Take: safeTake,
                Events: events.Take(safeTake).ToArray());

            return Results.Ok(response);
        })
        .WithName("GetOperationalAuditTrailRecent");

        return endpoints;
    }
}

public sealed record OperationalAuditTrailSummaryResponse(
    int TotalEvents,
    int SecurityEvents,
    int RuntimeEvents,
    int ConfigurationEvents,
    DateTimeOffset? LastEventUtc,
    string Status);

public sealed record OperationalAuditTrailRecentResponse(
    int Take,
    IReadOnlyList<OperationalAuditTrailEventResponse> Events);

public sealed record OperationalAuditTrailEventResponse(
    string EventId,
    DateTimeOffset OccurredUtc,
    string Category,
    string Action,
    string Actor,
    string ResourceType,
    string ResourceId,
    string Outcome,
    string? Message);
