using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunControlEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunControlEndpoints(
        this RouteGroupBuilder api)
    {
        api.MapGet(
            "/operational/runs/{runId:guid}/control-state",
            async (
                Guid runId,
                IOperationalRunControlService service,
                CancellationToken cancellationToken) =>
            {
                return Results.Ok(
                    await service.GetControlStateAsync(runId, cancellationToken));
            });

        api.MapPost(
            "/operational/runs/{runId:guid}/cancel",
            async (
                Guid runId,
                OperationalRunControlActionRequest request,
                IOperationalRunControlService service,
                CancellationToken cancellationToken) =>
            {
                return Results.Ok(
                    await service.RequestCancelAsync(runId, request, cancellationToken));
            });

        api.MapPost(
            "/operational/runs/{runId:guid}/abort",
            async (
                Guid runId,
                OperationalRunControlActionRequest request,
                IOperationalRunControlService service,
                CancellationToken cancellationToken) =>
            {
                return Results.Ok(
                    await service.AbortAsync(runId, request, cancellationToken));
            });

        return api;
    }
}
