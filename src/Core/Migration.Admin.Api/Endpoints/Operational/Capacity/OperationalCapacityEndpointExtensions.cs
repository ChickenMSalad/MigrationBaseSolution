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
            var capacity = BuildCapacity(snapshot);

            var response = new OperationalCapacitySummaryResponse(
                RuntimeStatus: snapshot.Status,
                RuntimePressure: capacity.RuntimePressure,
                QueueDepth: snapshot.QueueDepth,
                ActiveRuns: snapshot.ActiveRuns,
                ActiveWorkers: snapshot.ActiveWorkers,
                WorkerUtilizationPercent: capacity.WorkerUtilizationPercent,
                FailureCount: snapshot.FailureCount,
                Message: capacity.Message,
                LastUpdatedUtc: DateTimeOffset.UtcNow,
                Signals: capacity.Signals,
                RecommendedAction: capacity.RecommendedAction);

            return Results.Ok(response);
        })
        .WithName("GetOperationalCapacitySummary");

        group.MapGet("/runtime-preview", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var capacity = BuildCapacity(snapshot);

            var items = new List<OperationalRuntimePreviewItemResponse>
            {
                new(
                    Metric: "RuntimeStatus",
                    Value: snapshot.Status,
                    Severity: snapshot.Status.Equals("healthy", StringComparison.OrdinalIgnoreCase) ? "healthy" : "critical",
                    Description: snapshot.Message ?? "Operational SQL runtime status."),
                new(
                    Metric: "QueueDepth",
                    Value: snapshot.QueueDepth.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Severity: capacity.QueueSeverity,
                    Description: "Pending or active SQL work item backlog."),
                new(
                    Metric: "ActiveRuns",
                    Value: snapshot.ActiveRuns.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Severity: snapshot.ActiveRuns > 0 ? "active" : "healthy",
                    Description: "Runs currently not in a terminal state."),
                new(
                    Metric: "ActiveWorkers",
                    Value: snapshot.ActiveWorkers.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Severity: snapshot.QueueDepth > 0 && snapshot.ActiveWorkers == 0 ? "critical" : "healthy",
                    Description: "Workers inferred from SQL work item activity."),
                new(
                    Metric: "FailureCount",
                    Value: snapshot.FailureCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Severity: snapshot.FailureCount > 0 ? "critical" : "healthy",
                    Description: "Failed SQL work items requiring review or retry."),
                new(
                    Metric: "RecommendedAction",
                    Value: capacity.RecommendedAction,
                    Severity: capacity.RecommendedActionSeverity,
                    Description: capacity.Message)
            };

            var response = new OperationalRuntimePreviewResponse(
                GeneratedUtc: DateTimeOffset.UtcNow,
                Items: items);

            return Results.Ok(response);
        })
        .WithName("GetOperationalRuntimePreview");

        group.MapGet("/recommendations", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var capacity = BuildCapacity(snapshot);

            var response = new OperationalCapacityRecommendationResponse(
                GeneratedUtc: DateTimeOffset.UtcNow,
                RuntimePressure: capacity.RuntimePressure,
                RecommendedAction: capacity.RecommendedAction,
                Severity: capacity.RecommendedActionSeverity,
                Signals: capacity.Signals);

            return Results.Ok(response);
        })
        .WithName("GetOperationalCapacityRecommendations");

        return endpoints;
    }

    private static CapacityComputation BuildCapacity(SqlOperationalMetricsSnapshot snapshot)
    {
        var signals = new List<OperationalCapacitySignalResponse>();

        var queueSeverity = snapshot.QueueDepth switch
        {
            >= 1000 => "critical",
            >= 100 => "warning",
            > 0 => "active",
            _ => "healthy"
        };

        signals.Add(new OperationalCapacitySignalResponse(
            Name: "queue-depth",
            Value: snapshot.QueueDepth,
            Severity: queueSeverity,
            Description: "SQL work items waiting for dispatcher/executor capacity."));

        var workerSeverity = snapshot.QueueDepth > 0 && snapshot.ActiveWorkers == 0 ? "critical" : "healthy";
        signals.Add(new OperationalCapacitySignalResponse(
            Name: "worker-availability",
            Value: snapshot.ActiveWorkers,
            Severity: workerSeverity,
            Description: "Active worker count inferred from SQL operational state."));

        var failureSeverity = snapshot.FailureCount > 0 ? "critical" : "healthy";
        signals.Add(new OperationalCapacitySignalResponse(
            Name: "failures",
            Value: snapshot.FailureCount,
            Severity: failureSeverity,
            Description: "Failed work items reduce effective runtime capacity until retried or resolved."));

        var activeRunSeverity = snapshot.ActiveRuns > 0 ? "active" : "healthy";
        signals.Add(new OperationalCapacitySignalResponse(
            Name: "active-runs",
            Value: snapshot.ActiveRuns,
            Severity: activeRunSeverity,
            Description: "Runs currently in a non-terminal operational state."));

        var runtimePressure = ResolveRuntimePressure(snapshot);
        var utilization = ResolveWorkerUtilizationPercent(snapshot);
        var recommendation = ResolveRecommendation(snapshot, runtimePressure);
        var recommendationSeverity = ResolveRecommendationSeverity(runtimePressure, snapshot);
        var message = ResolveMessage(snapshot, runtimePressure, recommendation);

        return new CapacityComputation(
            RuntimePressure: runtimePressure,
            QueueSeverity: queueSeverity,
            WorkerUtilizationPercent: utilization,
            RecommendedAction: recommendation,
            RecommendedActionSeverity: recommendationSeverity,
            Message: message,
            Signals: signals);
    }

    private static string ResolveRuntimePressure(SqlOperationalMetricsSnapshot snapshot)
    {
        if (!snapshot.Status.Equals("healthy", StringComparison.OrdinalIgnoreCase))
        {
            return "unhealthy";
        }

        if (snapshot.QueueDepth > 0 && snapshot.ActiveWorkers == 0)
        {
            return "blocked";
        }

        if (snapshot.FailureCount > 0)
        {
            return "degraded";
        }

        if (snapshot.QueueDepth >= 1000)
        {
            return "critical";
        }

        if (snapshot.QueueDepth >= 100)
        {
            return "high";
        }

        if (snapshot.QueueDepth > 0)
        {
            return "elevated";
        }

        return "normal";
    }

    private static int ResolveWorkerUtilizationPercent(SqlOperationalMetricsSnapshot snapshot)
    {
        if (snapshot.QueueDepth <= 0 && snapshot.ActiveRuns <= 0)
        {
            return 0;
        }

        if (snapshot.ActiveWorkers <= 0)
        {
            return 100;
        }

        var estimatedCapacity = Math.Max(1, snapshot.ActiveWorkers * 100);
        var utilization = (int)Math.Ceiling(Math.Min(1.0d, snapshot.QueueDepth / (double)estimatedCapacity) * 100.0d);
        return Math.Max(1, utilization);
    }

    private static string ResolveRecommendation(SqlOperationalMetricsSnapshot snapshot, string runtimePressure)
    {
        if (!snapshot.Status.Equals("healthy", StringComparison.OrdinalIgnoreCase))
        {
            return "repair-sql-runtime";
        }

        if (snapshot.QueueDepth > 0 && snapshot.ActiveWorkers == 0)
        {
            return "start-or-restart-workers";
        }

        if (snapshot.FailureCount > 0)
        {
            return "review-failures";
        }

        if (runtimePressure is "critical" or "high")
        {
            return "scale-workers";
        }

        if (runtimePressure == "elevated")
        {
            return "monitor-throughput";
        }

        return "none";
    }

    private static string ResolveRecommendationSeverity(string runtimePressure, SqlOperationalMetricsSnapshot snapshot)
    {
        if (!snapshot.Status.Equals("healthy", StringComparison.OrdinalIgnoreCase) || runtimePressure is "blocked" or "critical" or "unhealthy")
        {
            return "critical";
        }

        if (runtimePressure is "high" or "degraded")
        {
            return "warning";
        }

        if (runtimePressure == "elevated")
        {
            return "active";
        }

        return "healthy";
    }

    private static string ResolveMessage(SqlOperationalMetricsSnapshot snapshot, string runtimePressure, string recommendation)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.Message) && !snapshot.Status.Equals("healthy", StringComparison.OrdinalIgnoreCase))
        {
            return snapshot.Message;
        }

        return runtimePressure switch
        {
            "blocked" => "Queued work exists but no active workers were inferred from SQL runtime state.",
            "degraded" => "Failures are present and should be reviewed before increasing throughput.",
            "critical" => "Queue depth is critical. Scale workers or pause new run intake.",
            "high" => "Queue depth is high. Consider increasing worker capacity.",
            "elevated" => "Runtime is active. Monitor throughput and failure rate.",
            "unhealthy" => "Operational SQL runtime is not healthy.",
            _ => recommendation == "none" ? "Runtime capacity is normal." : "Capacity recommendation is available."
        };
    }

    private sealed record CapacityComputation(
        string RuntimePressure,
        string QueueSeverity,
        int WorkerUtilizationPercent,
        string RecommendedAction,
        string RecommendedActionSeverity,
        string Message,
        IReadOnlyList<OperationalCapacitySignalResponse> Signals);
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
    DateTimeOffset LastUpdatedUtc,
    IReadOnlyList<OperationalCapacitySignalResponse> Signals,
    string RecommendedAction);

public sealed record OperationalRuntimePreviewResponse(
    DateTimeOffset GeneratedUtc,
    IReadOnlyList<OperationalRuntimePreviewItemResponse> Items);

public sealed record OperationalRuntimePreviewItemResponse(
    string Metric,
    string Value,
    string Severity,
    string Description);

public sealed record OperationalCapacityRecommendationResponse(
    DateTimeOffset GeneratedUtc,
    string RuntimePressure,
    string RecommendedAction,
    string Severity,
    IReadOnlyList<OperationalCapacitySignalResponse> Signals);

public sealed record OperationalCapacitySignalResponse(
    string Name,
    int Value,
    string Severity,
    string Description);
