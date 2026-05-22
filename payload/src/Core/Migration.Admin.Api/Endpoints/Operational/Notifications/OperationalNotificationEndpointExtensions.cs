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
            var criticalAlerts = snapshot.FailureCount > 0 ? 1 : 0;
            var pendingAlerts = criticalAlerts + (snapshot.QueueDepth > 0 ? 1 : 0);

            var response = new OperationalNotificationSummaryResponse(
                TotalRoutes: 0,
                EnabledRoutes: 0,
                PendingAlerts: pendingAlerts,
                CriticalAlerts: criticalAlerts,
                Status: snapshot.Status);

            return Results.Ok(response);
        })
        .WithName("GetOperationalNotificationSummary");

        group.MapGet("/routes", () =>
        {
            var response = new OperationalNotificationRoutesResponse(
                Routes: Array.Empty<OperationalNotificationRouteResponse>());

            return Results.Ok(response);
        })
        .WithName("GetOperationalNotificationRoutes");

        group.MapGet("/alert-preview", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var alerts = new List<OperationalAlertPreviewItemResponse>();

            if (snapshot.FailureCount > 0)
            {
                alerts.Add(new OperationalAlertPreviewItemResponse(
                    AlertId: "sql-failures",
                    CreatedUtc: DateTimeOffset.UtcNow,
                    Severity: "critical",
                    Category: "runtime",
                    Title: "Migration failures detected",
                    Message: $"{snapshot.FailureCount} migration failure record(s) exist in the operational store.",
                    Source: "OperationalSql"));
            }

            if (snapshot.QueueDepth > 0)
            {
                alerts.Add(new OperationalAlertPreviewItemResponse(
                    AlertId: "sql-queue-depth",
                    CreatedUtc: DateTimeOffset.UtcNow,
                    Severity: "warning",
                    Category: "queue",
                    Title: "Operational queue has pending work",
                    Message: $"{snapshot.QueueDepth} work item(s) exist in the operational queue.",
                    Source: "OperationalSql"));
            }

            if (snapshot.Status is "not-configured" or "unhealthy")
            {
                alerts.Add(new OperationalAlertPreviewItemResponse(
                    AlertId: "sql-health",
                    CreatedUtc: DateTimeOffset.UtcNow,
                    Severity: "critical",
                    Category: "infrastructure",
                    Title: "Operational SQL is not healthy",
                    Message: snapshot.Message ?? "Operational SQL metrics reader is not healthy.",
                    Source: "OperationalSql"));
            }

            var response = new OperationalAlertPreviewResponse(Alerts: alerts);
            return Results.Ok(response);
        })
        .WithName("GetOperationalAlertPreview");

        return endpoints;
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
