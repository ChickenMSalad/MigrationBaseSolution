using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Migration.Admin.Api.Endpoints.Operational.Notifications;

public static class OperationalNotificationEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/notifications")
            .WithTags("Operational Notifications");

        group.MapGet("/summary", () =>
        {
            var response = new OperationalNotificationSummaryResponse(
                TotalRoutes: 0,
                EnabledRoutes: 0,
                PendingAlerts: 0,
                CriticalAlerts: 0,
                Status: "not-wired");

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

        group.MapGet("/alert-preview", () =>
        {
            var response = new OperationalAlertPreviewResponse(
                Alerts: Array.Empty<OperationalAlertPreviewItemResponse>());

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
