using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatcherDiagnosticsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatcherDiagnosticsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/dispatcher/diagnostics",
                async (
                    IOperationalDispatcherDiagnosticsService diagnosticsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await diagnosticsService.GetDiagnosticsAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalDispatcherDiagnostics")
            .WithTags("Operational Store")
            .WithSummary("Gets operational dispatcher leasing diagnostics.")
            .Produces<OperationalDispatcherDiagnosticsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


