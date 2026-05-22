using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Endpoints.Operational.Events;

public static class OperationalEventRetentionEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalEventRetentionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/events/retention")
            .WithTags("Operational Event Retention");

        group.MapGet("/policy", (IOptions<OperationalEventRetentionOptions> options) =>
        {
            var value = options.Value;

            return Results.Ok(new OperationalEventRetentionPolicyResponse(
                Enabled: value.Enabled,
                RetentionDays: value.RetentionDays,
                IntervalHours: value.IntervalHours,
                StartupDelaySeconds: value.StartupDelaySeconds));
        })
        .WithName("GetOperationalEventRetentionPolicy");

        group.MapPost("/prune", async (
            IOperationalEventRetentionService retentionService,
            IOptions<OperationalEventRetentionOptions> options,
            CancellationToken cancellationToken) =>
        {
            var retentionDays = Math.Clamp(options.Value.RetentionDays, 1, 3650);
            var result = await retentionService.PruneAsync(retentionDays, cancellationToken);

            return Results.Ok(result);
        })
        .WithName("PruneOperationalEvents");

        return endpoints;
    }
}

public sealed record OperationalEventRetentionPolicyResponse(
    bool Enabled,
    int RetentionDays,
    int IntervalHours,
    int StartupDelaySeconds);
