using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.SqlMetrics;

namespace Migration.Admin.Api.Endpoints.Operational.Capacity;

public static class OperationalCapacityEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalCapacityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/capacity")
            .WithTags("Operational Capacity");

        group.MapGet("/summary", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);

            var runtimePressure = snapshot.QueueDepth > 100
                ? "high"
                : snapshot.QueueDepth > 0
                    ? "elevated"
                    : "normal";

            var workerUtilizationPercent = snapshot.QueueDepth > 0
                ? Math.Min(100, snapshot.QueueDepth)
                : 0;

            var response = new OperationalCapacitySummaryResponse(
                RuntimeStatus: snapshot.Status,
                RuntimePressure: runtimePressure,
                QueueDepth: snapshot.QueueDepth,
                ActiveRuns: snapshot.ActiveRuns,
                ActiveWorkers: snapshot.ActiveWorkers,
                WorkerUtilizationPercent: workerUtilizationPercent,
                FailureCount: snapshot.FailureCount,
                Message: snapshot.Message,
                LastUpdatedUtc: DateTimeOffset.UtcNow);

            return Results.Ok(response);
        })
        .WithName("GetOperationalCapacitySummary");

        group.MapGet("/runtime-preview", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);

            var items = new List<OperationalRuntimePreviewItemResponse>();

            items.Add(new OperationalRuntimePreviewItemResponse(
                Metric: "QueueDepth",
                Value: snapshot.QueueDepth.ToString(),
                Severity: snapshot.QueueDepth > 100
                    ? "critical"
                    : snapshot.QueueDepth > 0
                        ? "warning"
                        : "healthy",
                Description: "Operational work item queue depth."));

            items.Add(new OperationalRuntimePreviewItemResponse(
                Metric: "FailureCount",
                Value: snapshot.FailureCount.ToString(),
                Severity: snapshot.FailureCount > 0 ? "critical" : "healthy",
                Description: "Persisted migration failures."));

            items.Add(new OperationalRuntimePreviewItemResponse(
                Metric: "RuntimeStatus",
                Value: snapshot.Status,
                Severity: snapshot.Status == "healthy" ? "healthy" : "critical",
                Description: snapshot.Message ?? "Operational SQL runtime status."));

            var response = new OperationalRuntimePreviewResponse(
                GeneratedUtc: DateTimeOffset.UtcNow,
                Items: items);

            return Results.Ok(response);
        })
        .WithName("GetOperationalRuntimePreview");

        return endpoints;
    }
}

public sealed record OperationalCapacitySummaryResponse(
    string RuntimeStatus,
    string RuntimePressure,
    int QueueDepth,
    int ActiveRuns,
    int ActiveWorkers,
    int WorkerUtilizationPercent,
    int FailureCount,
    string? Message,
    DateTimeOffset LastUpdatedUtc);

public sealed record OperationalRuntimePreviewResponse(
    DateTimeOffset GeneratedUtc,
    IReadOnlyList<OperationalRuntimePreviewItemResponse> Items);

public sealed record OperationalRuntimePreviewItemResponse(
    string Metric,
    string Value,
    string Severity,
    string Description);


