using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunControlEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunControlEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/{runId:guid}/control-state",
                async (
                    Guid runId,
                    IOperationalRunControlService service,
                    CancellationToken cancellationToken) =>
                {
                    var response = await service.GetControlStateAsync(runId, cancellationToken);

                    return response.CurrentStatus == "Unknown"
                        ? Results.NotFound(response)
                        : Results.Ok(response);
                })
            .WithName("GetOperationalRunControlState")
            .WithTags("Operational Store")
            .WithSummary("Gets operational run control state.")
            .Produces<OperationalRunControlStateResponse>(StatusCodes.Status200OK)
            .Produces<OperationalRunControlStateResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        api.MapPost(
                "/operational/runs/{runId:guid}/cancel",
                async (
                    Guid runId,
                    OperationalRunControlActionRequest request,
                    IOperationalRunControlService service,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await service.RequestCancelAsync(runId, request, cancellationToken);

                        return response.CurrentStatus == "Unknown"
                            ? Results.NotFound(response)
                            : Results.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("CancelOperationalRun")
            .WithTags("Operational Store")
            .WithSummary("Requests graceful cancellation for an operational run.")
            .Produces<OperationalRunControlStateResponse>(StatusCodes.Status200OK)
            .Produces<OperationalRunControlStateResponse>(StatusCodes.Status404NotFound)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/runs/{runId:guid}/abort",
                async (
                    Guid runId,
                    OperationalRunControlActionRequest request,
                    IOperationalRunControlService service,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await service.AbortAsync(runId, request, cancellationToken);

                        return response.CurrentStatus == "Unknown"
                            ? Results.NotFound(response)
                            : Results.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("AbortOperationalRun")
            .WithTags("Operational Store")
            .WithSummary("Aborts an operational run and releases locked work items.")
            .Produces<OperationalRunControlStateResponse>(StatusCodes.Status200OK)
            .Produces<OperationalRunControlStateResponse>(StatusCodes.Status404NotFound)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/runs/{runId:guid}/resume",
                async (
                    Guid runId,
                    OperationalRunControlActionRequest request,
                    IOperationalRunControlService service,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await service.ResumeAsync(runId, request, cancellationToken);

                        return response.CurrentStatus == "Unknown"
                            ? Results.NotFound(response)
                            : Results.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("ResumeOperationalRun")
            .WithTags("Operational Store")
            .WithSummary("Resumes a cancel-requested operational run.")
            .Produces<OperationalRunControlStateResponse>(StatusCodes.Status200OK)
            .Produces<OperationalRunControlStateResponse>(StatusCodes.Status404NotFound)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return api;
    }
}


