using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Application.Operational.Health;

namespace Migration.Admin.Api.Endpoints.Operational.Health;

public static class OperationalHealthProbeEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalHealthProbeEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var health = group.MapGroup("/health")
            .WithTags("Operational Health");

        health.MapGet("/live", async (
                IOperationalHealthProbeService healthProbeService,
                CancellationToken cancellationToken) =>
            {
                var response = await healthProbeService.GetLivenessAsync(cancellationToken).ConfigureAwait(false);
                return Results.Ok(response);
            })
            .WithName("GetOperationalHealthLiveness")
            .Produces<OperationalHealthProbeResponse>(StatusCodes.Status200OK);

        health.MapGet("/ready", async (
                IOperationalHealthProbeService healthProbeService,
                CancellationToken cancellationToken) =>
            {
                var response = await healthProbeService.GetReadinessAsync(cancellationToken).ConfigureAwait(false);
                return Results.Ok(response);
            })
            .WithName("GetOperationalHealthReadiness")
            .Produces<OperationalHealthProbeResponse>(StatusCodes.Status200OK);

        return group;
    }
}
