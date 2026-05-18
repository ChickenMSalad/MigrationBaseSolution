using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalWorkItemLeaseEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalWorkItemLeaseEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapPost(
                "/operational/work-items/lease",
                async (
                    OperationalWorkItemLeaseRequest request,
                    IOperationalWorkItemLeaseService leaseService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await leaseService.LeaseAsync(
                            request,
                            cancellationToken);

                        return Results.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("LeaseOperationalWorkItems")
            .WithTags("Operational Store")
            .WithSummary("Leases unlocked operational work items for a worker.")
            .Produces<OperationalWorkItemLeaseResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/work-items/{workItemId:guid}/heartbeat",
                async (
                    Guid workItemId,
                    OperationalWorkItemHeartbeatRequest request,
                    IOperationalWorkItemLeaseService leaseService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await leaseService.HeartbeatAsync(
                            workItemId,
                            request,
                            cancellationToken);

                        return response.Success
                            ? Results.Ok(response)
                            : Results.Conflict(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("HeartbeatOperationalWorkItem")
            .WithTags("Operational Store")
            .WithSummary("Refreshes the lock timestamp for a leased operational work item.")
            .Produces<OperationalWorkItemStateTransitionResponse>(StatusCodes.Status200OK)
            .Produces<OperationalWorkItemStateTransitionResponse>(StatusCodes.Status409Conflict)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/work-items/{workItemId:guid}/complete",
                async (
                    Guid workItemId,
                    OperationalWorkItemCompleteRequest request,
                    IOperationalWorkItemLeaseService leaseService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await leaseService.CompleteAsync(
                            workItemId,
                            request,
                            cancellationToken);

                        return response.Success
                            ? Results.Ok(response)
                            : Results.Conflict(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("CompleteOperationalWorkItem")
            .WithTags("Operational Store")
            .WithSummary("Marks a leased operational work item completed.")
            .Produces<OperationalWorkItemStateTransitionResponse>(StatusCodes.Status200OK)
            .Produces<OperationalWorkItemStateTransitionResponse>(StatusCodes.Status409Conflict)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/work-items/{workItemId:guid}/fail",
                async (
                    Guid workItemId,
                    OperationalWorkItemFailRequest request,
                    IOperationalWorkItemLeaseService leaseService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await leaseService.FailAsync(
                            workItemId,
                            request,
                            cancellationToken);

                        return response.Success
                            ? Results.Ok(response)
                            : Results.Conflict(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("FailOperationalWorkItem")
            .WithTags("Operational Store")
            .WithSummary("Marks a leased operational work item failed.")
            .Produces<OperationalWorkItemStateTransitionResponse>(StatusCodes.Status200OK)
            .Produces<OperationalWorkItemStateTransitionResponse>(StatusCodes.Status409Conflict)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return api;
    }
}
