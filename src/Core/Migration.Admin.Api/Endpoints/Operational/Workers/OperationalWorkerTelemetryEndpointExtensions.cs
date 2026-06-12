using Microsoft.AspNetCore.Mvc;

namespace Migration.Admin.Api.Endpoints.Operational.Workers;

public static class OperationalWorkerTelemetryEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalWorkerTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/operational/workers")
            .WithTags("Operational Workers");

        group.MapGet("/telemetry", GetTelemetry)
            .WithName("GetOperationalWorkerTelemetry")
            .WithSummary("Returns current worker, queue, and lease telemetry for the SQL-backed runtime.");

        group.MapGet("/leases", GetLeases)
            .WithName("GetOperationalWorkerLeases")
            .WithSummary("Returns current worker lease ownership and stale lease projections.");

        return app;
    }

    private static IResult GetTelemetry([FromQuery] string? runId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var response = new OperationalWorkerTelemetryResponse(
            GeneratedUtc: now,
            RunId: runId,
            Workers:
            [
                new OperationalWorkerTelemetryItem("dispatcher", "online", now.AddSeconds(-8), 0, 0, "Dispatch queue watcher"),
                new OperationalWorkerTelemetryItem("executor-1", "online", now.AddSeconds(-14), 3, 2, "Service Bus executor"),
                new OperationalWorkerTelemetryItem("executor-2", "warming", now.AddSeconds(-45), 0, 0, "Service Bus executor")
            ],
            Queue: new OperationalWorkerQueueTelemetry(Ready: 0, Leased: 0, InFlight: 2, Failed: 0, Completed: 0),
            Warnings:
            [
                "P4.17 endpoint is a control-plane telemetry facade. Wire to SQL lease/work-item projections when live runtime data is enabled."
            ]);

        return Results.Ok(response);
    }

    private static IResult GetLeases([FromQuery] string? runId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var response = new OperationalWorkerLeaseResponse(
            GeneratedUtc: now,
            RunId: runId,
            Leases:
            [
                new OperationalWorkerLeaseItem("lease-dispatcher", "dispatcher", "active", now.AddMinutes(3), 180),
                new OperationalWorkerLeaseItem("lease-executor-1", "executor-1", "active", now.AddMinutes(2), 120)
            ]);

        return Results.Ok(response);
    }
}

public sealed record OperationalWorkerTelemetryResponse(
    DateTimeOffset GeneratedUtc,
    string? RunId,
    IReadOnlyCollection<OperationalWorkerTelemetryItem> Workers,
    OperationalWorkerQueueTelemetry Queue,
    IReadOnlyCollection<string> Warnings);

public sealed record OperationalWorkerTelemetryItem(
    string WorkerId,
    string Status,
    DateTimeOffset LastHeartbeatUtc,
    int ActiveLeases,
    int InFlightWorkItems,
    string Role);

public sealed record OperationalWorkerQueueTelemetry(
    int Ready,
    int Leased,
    int InFlight,
    int Failed,
    int Completed);

public sealed record OperationalWorkerLeaseResponse(
    DateTimeOffset GeneratedUtc,
    string? RunId,
    IReadOnlyCollection<OperationalWorkerLeaseItem> Leases);

public sealed record OperationalWorkerLeaseItem(
    string LeaseId,
    string WorkerId,
    string Status,
    DateTimeOffset ExpiresUtc,
    int SecondsRemaining);


