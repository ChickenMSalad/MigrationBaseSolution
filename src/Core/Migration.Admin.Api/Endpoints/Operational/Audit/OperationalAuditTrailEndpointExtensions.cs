using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Migration.Admin.Api.Endpoints.Operational.Audit;

public static class OperationalAuditTrailEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalAuditTrailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/audit-trail")
            .WithTags("Operational Audit Trail");

        group.MapGet("/summary", () =>
        {
            var response = new OperationalAuditTrailSummaryResponse(
                TotalEvents: 0,
                SecurityEvents: 0,
                RuntimeEvents: 0,
                ConfigurationEvents: 0,
                LastEventUtc: null,
                Status: "not-wired");

            return Results.Ok(response);
        })
        .WithName("GetOperationalAuditTrailSummary");

        group.MapGet("/recent", (int? take) =>
        {
            var safeTake = Math.Clamp(take.GetValueOrDefault(50), 1, 250);

            var response = new OperationalAuditTrailRecentResponse(
                Take: safeTake,
                Events: Array.Empty<OperationalAuditTrailEventResponse>());

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
