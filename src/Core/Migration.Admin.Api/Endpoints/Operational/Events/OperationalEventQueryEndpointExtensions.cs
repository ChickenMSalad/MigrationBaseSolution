using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Endpoints.Operational.Events;

public static class OperationalEventQueryEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalEventQueryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/events/query")
            .WithTags("Operational Event Query");

        group.MapGet("/", async (
            IOperationalEventQueryService queryService,
            string? severity,
            string? category,
            string? eventType,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int? skip,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var request = new OperationalEventQueryRequest(
                Severity: Normalize(severity),
                Category: Normalize(category),
                EventType: Normalize(eventType),
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Skip: Math.Max(0, skip.GetValueOrDefault(0)),
                Take: Math.Clamp(take.GetValueOrDefault(50), 1, 250));

            var events = await queryService.QueryAsync(request, cancellationToken);

            return Results.Ok(new OperationalEventQueryResponse(
                Skip: request.Skip,
                Take: request.Take,
                Returned: events.Count,
                Events: events));
        })
        .WithName("QueryOperationalEvents");

        group.MapGet("/summary", async (
            IOperationalEventQueryService queryService,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            CancellationToken cancellationToken) =>
        {
            var summary = await queryService.ReadAggregateSummaryAsync(
                fromUtc,
                toUtc,
                cancellationToken);

            return Results.Ok(summary);
        })
        .WithName("GetOperationalEventQuerySummary");

        return endpoints;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
