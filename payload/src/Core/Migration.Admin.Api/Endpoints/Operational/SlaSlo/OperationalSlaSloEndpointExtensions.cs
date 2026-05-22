using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Migration.Admin.Api.Endpoints.Operational.SlaSlo;

public static class OperationalSlaSloEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalSlaSloEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/sla-slo")
            .WithTags("Operational SLA/SLO");

        group.MapGet("/summary", () =>
        {
            var response = new OperationalSlaSloSummaryResponse(
                TotalPolicies: 0,
                ActivePolicies: 0,
                WarningBreaches: 0,
                CriticalBreaches: 0,
                Status: "not-wired");

            return Results.Ok(response);
        })
        .WithName("GetOperationalSlaSloSummary");

        group.MapGet("/policies", () =>
        {
            var response = new OperationalSlaSloPolicyCatalogResponse(
                Policies: Array.Empty<OperationalSlaSloPolicyResponse>());

            return Results.Ok(response);
        })
        .WithName("GetOperationalSlaSloPolicies");

        group.MapGet("/breach-preview", () =>
        {
            var response = new OperationalSlaSloBreachPreviewResponse(
                Breaches: Array.Empty<OperationalSlaSloBreachPreviewItemResponse>());

            return Results.Ok(response);
        })
        .WithName("GetOperationalSlaSloBreachPreview");

        return endpoints;
    }
}

public sealed record OperationalSlaSloSummaryResponse(
    int TotalPolicies,
    int ActivePolicies,
    int WarningBreaches,
    int CriticalBreaches,
    string Status);

public sealed record OperationalSlaSloPolicyCatalogResponse(
    IReadOnlyList<OperationalSlaSloPolicyResponse> Policies);

public sealed record OperationalSlaSloPolicyResponse(
    string PolicyId,
    string Name,
    string Metric,
    string Threshold,
    string Severity,
    bool Enabled,
    string? Description);

public sealed record OperationalSlaSloBreachPreviewResponse(
    IReadOnlyList<OperationalSlaSloBreachPreviewItemResponse> Breaches);

public sealed record OperationalSlaSloBreachPreviewItemResponse(
    string BreachId,
    DateTimeOffset DetectedUtc,
    string Severity,
    string Metric,
    string Threshold,
    string ObservedValue,
    string Scope,
    string Message);
