using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Events;
using Migration.Admin.Api.Operational.SqlMetrics;

namespace Migration.Admin.Api.Endpoints.Operational.Events;

public static class OperationalEventEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/events")
            .WithTags("Operational Events");

        group.MapPost("/snapshot", async (
            ISqlOperationalMetricsReader metricsReader,
            IOperationalEventStore eventStore,
            OperationalEventSnapshotRequest? request,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var severity = snapshot.Status == "healthy" ? "info" : "warning";

            if (snapshot.FailureCount > 0 || snapshot.Status == "unhealthy")
            {
                severity = "critical";
            }

            var eventId = await eventStore.WriteAsync(
                eventType: "OperationalMetricsSnapshot",
                severity: severity,
                category: "runtime",
                source: "Migration.Admin.Api",
                message: $"Operational metrics snapshot recorded with status '{snapshot.Status}'.",
                payloadJson: JsonSerializer.Serialize(snapshot),
                executionSessionId: request?.ExecutionSessionId,
                migrationRunId: request?.MigrationRunId,
                cancellationToken: cancellationToken);

            return Results.Ok(new OperationalEventSnapshotResponse(
                OperationalEventId: eventId,
                Status: snapshot.Status,
                Severity: severity,
                ExecutionSessionId: request?.ExecutionSessionId,
                MigrationRunId: request?.MigrationRunId,
                CreatedUtc: DateTimeOffset.UtcNow));
        })
        .WithName("RecordOperationalMetricsSnapshot");

        group.MapGet("/recent", async (
            IOperationalEventStore eventStore,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var safeTake = Math.Clamp(take.GetValueOrDefault(50), 1, 250);
            var events = await eventStore.ReadRecentAsync(safeTake, cancellationToken);

            return Results.Ok(new OperationalRecentEventsResponse(
                Take: safeTake,
                Events: events));
        })
        .WithName("GetRecentOperationalEvents");

        return endpoints;
    }
}

public sealed record OperationalEventSnapshotRequest(
    Guid? ExecutionSessionId,
    Guid? MigrationRunId);

public sealed record OperationalEventSnapshotResponse(
    Guid OperationalEventId,
    string Status,
    string Severity,
    Guid? ExecutionSessionId,
    Guid? MigrationRunId,
    DateTimeOffset CreatedUtc);

public sealed record OperationalRecentEventsResponse(
    int Take,
    IReadOnlyList<OperationalEventRecord> Events);
