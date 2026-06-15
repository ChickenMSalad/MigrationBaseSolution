using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.SqlMetrics;

namespace Migration.Admin.Api.Endpoints.Operational.Notifications;

public static class OperationalNotificationEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/notifications")
            .WithTags("Operational Notifications");

        group.MapGet("/summary", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var alerts = BuildAlerts(snapshot);
            var routes = BuildRoutes();

            var response = new OperationalNotificationSummaryResponse(
                TotalRoutes: routes.Count,
                EnabledRoutes: routes.Count(r => r.Enabled),
                PendingAlerts: alerts.Count,
                CriticalAlerts: alerts.Count(a => string.Equals(a.Severity, "critical", StringComparison.OrdinalIgnoreCase)),
                Status: ResolveStatus(snapshot, alerts));

            return Results.Ok(response);
        })
        .WithName("GetOperationalNotificationSummary");

        group.MapGet("/routes", () =>
        {
            var response = new OperationalNotificationRoutesResponse(
                Routes: BuildRoutes());

            return Results.Ok(response);
        })
        .WithName("GetOperationalNotificationRoutes");

        group.MapGet("/alert-preview", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var response = new OperationalAlertPreviewResponse(
                Alerts: BuildAlerts(snapshot));

            return Results.Ok(response);
        })
        .WithName("GetOperationalAlertPreview");

        return endpoints;
    }

    private static string ResolveStatus(SqlOperationalMetricsSnapshot snapshot, IReadOnlyList<OperationalAlertPreviewItemResponse> alerts)
    {
        if (snapshot.Status is "not-configured" or "unhealthy")
        {
            return snapshot.Status;
        }

        if (alerts.Any(a => string.Equals(a.Severity, "critical", StringComparison.OrdinalIgnoreCase)))
        {
            return "attention-required";
        }

        if (alerts.Count > 0)
        {
            return "warning";
        }

        return snapshot.Status;
    }

    private static IReadOnlyList<OperationalNotificationRouteResponse> BuildRoutes()
    {
        return new[]
        {
            new OperationalNotificationRouteResponse(
                RouteId: "runtime-critical",
                Name: "Runtime critical alerts",
                Severity: "critical",
                Channel: "admin-ui",
                Enabled: true,
                DestinationReference: "/operations/notifications",
                Description: "Critical runtime failures, unhealthy SQL readiness, and failed work item signals surfaced in the Admin UI."),
            new OperationalNotificationRouteResponse(
                RouteId: "runtime-warning",
                Name: "Runtime warning alerts",
                Severity: "warning",
                Channel: "admin-ui",
                Enabled: true,
                DestinationReference: "/operations/notifications",
                Description: "Queue pressure and active backlog signals surfaced in the Admin UI."),
            new OperationalNotificationRouteResponse(
                RouteId: "runtime-info",
                Name: "Runtime informational alerts",
                Severity: "info",
                Channel: "admin-ui",
                Enabled: true,
                DestinationReference: "/operations/notifications",
                Description: "Healthy runtime and activity status derived from SQL operational state.")
        };
    }

    private static IReadOnlyList<OperationalAlertPreviewItemResponse> BuildAlerts(SqlOperationalMetricsSnapshot snapshot)
    {
        var alerts = new List<OperationalAlertPreviewItemResponse>();
        var now = DateTimeOffset.UtcNow;

        if (snapshot.Status is "not-configured" or "unhealthy")
        {
            alerts.Add(new OperationalAlertPreviewItemResponse(
                AlertId: "sql-health",
                CreatedUtc: now,
                Severity: "critical",
                Category: "infrastructure",
                Title: "Operational SQL is not healthy",
                Message: snapshot.Message ?? "Operational SQL metrics reader is not healthy.",
                Source: "OperationalSql"));
        }

        if (snapshot.FailureCount > 0)
        {
            alerts.Add(new OperationalAlertPreviewItemResponse(
                AlertId: "sql-failures",
                CreatedUtc: now,
                Severity: "critical",
                Category: "runtime",
                Title: "Migration failures detected",
                Message: string.Concat(snapshot.FailureCount.ToString(System.Globalization.CultureInfo.InvariantCulture), " failed or faulted work item record(s) exist in the operational store."),
                Source: "migration.WorkItems"));
        }

        if (snapshot.QueueDepth > 0)
        {
            alerts.Add(new OperationalAlertPreviewItemResponse(
                AlertId: "sql-queue-depth",
                CreatedUtc: now,
                Severity: snapshot.QueueDepth >= 1000 ? "critical" : "warning",
                Category: "queue",
                Title: "Operational queue has pending work",
                Message: string.Concat(snapshot.QueueDepth.ToString(System.Globalization.CultureInfo.InvariantCulture), " work item record(s) currently exist in the operational queue."),
                Source: "migration.WorkItems"));
        }

        if (snapshot.ActiveRuns > 0)
        {
            alerts.Add(new OperationalAlertPreviewItemResponse(
                AlertId: "active-runs",
                CreatedUtc: now,
                Severity: "info",
                Category: "runtime",
                Title: "Migration runs are active",
                Message: string.Concat(snapshot.ActiveRuns.ToString(System.Globalization.CultureInfo.InvariantCulture), " operational run(s) are currently active."),
                Source: "migration.Runs"));
        }

        if (alerts.Count == 0 && string.Equals(snapshot.Status, "healthy", StringComparison.OrdinalIgnoreCase))
        {
            alerts.Add(new OperationalAlertPreviewItemResponse(
                AlertId: "runtime-healthy",
                CreatedUtc: now,
                Severity: "info",
                Category: "runtime",
                Title: "Operational runtime is healthy",
                Message: "No critical SQL runtime alerts are currently detected.",
                Source: "OperationalSql"));
        }

        return alerts;
    }
}

public sealed record OperationalNotificationSummaryResponse(
    int TotalRoutes,
    int EnabledRoutes,
    int PendingAlerts,
    int CriticalAlerts,
    string Status);

public sealed record OperationalNotificationRoutesResponse(
    IReadOnlyList<OperationalNotificationRouteResponse> Routes);

public sealed record OperationalNotificationRouteResponse(
    string RouteId,
    string Name,
    string Severity,
    string Channel,
    bool Enabled,
    string DestinationReference,
    string? Description);

public sealed record OperationalAlertPreviewResponse(
    IReadOnlyList<OperationalAlertPreviewItemResponse> Alerts);

public sealed record OperationalAlertPreviewItemResponse(
    string AlertId,
    DateTimeOffset CreatedUtc,
    string Severity,
    string Category,
    string Title,
    string Message,
    string Source);
