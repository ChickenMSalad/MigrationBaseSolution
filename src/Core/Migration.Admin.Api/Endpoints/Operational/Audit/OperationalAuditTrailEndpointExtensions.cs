using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Events;
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
            IOperationalEventStore eventStore,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var recentEvents = await ReadRecentEventsSafelyAsync(eventStore, 250, cancellationToken);

            var runtimeEvents = recentEvents.Count(e =>
                string.Equals(e.Category, "runtime", StringComparison.OrdinalIgnoreCase));

            var configurationEvents = recentEvents.Count(e =>
                string.Equals(e.Category, "configuration", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.Category, "infrastructure", StringComparison.OrdinalIgnoreCase));

            var securityEvents = recentEvents.Count(e =>
                string.Equals(e.Category, "security", StringComparison.OrdinalIgnoreCase));

            if (recentEvents.Count == 0)
            {
                runtimeEvents += snapshot.QueueDepth > 0 ? 1 : 0;
                runtimeEvents += snapshot.FailureCount > 0 ? 1 : 0;
                configurationEvents += snapshot.Status is "not-configured" or "unhealthy" ? 1 : 0;
            }

            var totalEvents = runtimeEvents + configurationEvents + securityEvents;
            DateTimeOffset? lastEventUtc = recentEvents.Count > 0
                ? recentEvents.Max(e => e.CreatedUtc)
                : totalEvents > 0
                    ? DateTimeOffset.UtcNow
                    : null;

            var response = new OperationalAuditTrailSummaryResponse(
                TotalEvents: totalEvents,
                SecurityEvents: securityEvents,
                RuntimeEvents: runtimeEvents,
                ConfigurationEvents: configurationEvents,
                LastEventUtc: lastEventUtc,
                Status: snapshot.Status);

            return Results.Ok(response);
        })
        .WithName("GetOperationalAuditTrailSummary");

        group.MapGet("/recent", async (
            ISqlOperationalMetricsReader metricsReader,
            IOperationalEventStore eventStore,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var safeTake = Math.Clamp(take.GetValueOrDefault(50), 1, 250);
            var persistedEvents = await ReadRecentEventsSafelyAsync(eventStore, safeTake, cancellationToken);

            if (persistedEvents.Count > 0)
            {
                var response = new OperationalAuditTrailRecentResponse(
                    Take: safeTake,
                    Events: persistedEvents
                        .Select(ToAuditTrailEvent)
                        .ToArray());

                return Results.Ok(response);
            }

            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var liveEvents = CreateLiveFallbackEvents(snapshot);

            return Results.Ok(new OperationalAuditTrailRecentResponse(
                Take: safeTake,
                Events: liveEvents.Take(safeTake).ToArray()));
        })
        .WithName("GetOperationalAuditTrailRecent");

        return endpoints;
    }

    private static async Task<IReadOnlyList<OperationalEventRecord>> ReadRecentEventsSafelyAsync(
        IOperationalEventStore eventStore,
        int take,
        CancellationToken cancellationToken)
    {
        try
        {
            return await eventStore.ReadRecentAsync(take, cancellationToken);
        }
        catch
        {
            return Array.Empty<OperationalEventRecord>();
        }
    }

    private static OperationalAuditTrailEventResponse ToAuditTrailEvent(
        OperationalEventRecord record)
    {
        return new OperationalAuditTrailEventResponse(
            EventId: record.OperationalEventId.ToString("D"),
            OccurredUtc: record.CreatedUtc,
            Category: record.Category,
            Action: record.EventType,
            Actor: "system",
            ResourceType: record.Source,
            ResourceId: record.OperationalEventId.ToString("D"),
            Outcome: record.Severity,
            Message: record.Message);
    }

    private static IReadOnlyList<OperationalAuditTrailEventResponse> CreateLiveFallbackEvents(
        SqlOperationalMetricsSnapshot snapshot)
    {
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

        return events;
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
