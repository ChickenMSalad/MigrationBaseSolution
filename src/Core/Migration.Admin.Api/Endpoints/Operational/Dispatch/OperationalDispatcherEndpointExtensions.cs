using Migration.Admin.Api.OperationalStore;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatcherEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatcherEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/dispatcher/status",
                (IOptions<OperationalDispatcherOptions> options) =>
                {
                    var value = options.Value;

                    return Results.Ok(new OperationalDispatcherStatusResponse
                    {
                        Enabled = value.Enabled,
                        WorkerId = string.IsNullOrWhiteSpace(value.WorkerId)
                            ? "local-dispatcher"
                            : value.WorkerId,
                        PollingIntervalSeconds = value.PollingIntervalSeconds,
                        LeaseCount = value.LeaseCount,
                        SimulateExecution = value.SimulateExecution,
                        Mode = value.Enabled ? "Enabled" : "Disabled"
                    });
                })
            .WithName("GetOperationalDispatcherStatus")
            .WithTags("Operational Store")
            .WithSummary("Gets operational dispatcher status.")
            .Produces<OperationalDispatcherStatusResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapPost(
                "/operational/dispatcher/run-once",
                async (
                    IOperationalDispatcherService dispatcherService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await dispatcherService.RunOnceAsync(cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("RunOperationalDispatcherOnce")
            .WithTags("Operational Store")
            .WithSummary("Runs one operational dispatcher lease/process/finalize cycle.")
            .Produces<OperationalDispatcherRunOnceResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
