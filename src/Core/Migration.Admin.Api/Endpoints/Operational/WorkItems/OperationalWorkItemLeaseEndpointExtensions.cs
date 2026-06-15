using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalWorkItemLeaseEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalWorkItemLeaseEndpoints(this RouteGroupBuilder api)
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
                        var response = await leaseService.LeaseAsync(request, cancellationToken);
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
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/work-items/{workItemId:long}/heartbeat",
                async (
                    long workItemId,
                    OperationalWorkItemHeartbeatRequest request,
                    IOperationalWorkItemLeaseService leaseService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await leaseService.HeartbeatAsync(workItemId, request, cancellationToken);
                        return response.Success ? Results.Ok(response) : Results.Conflict(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("HeartbeatOperationalWorkItem")
            .WithTags("Operational Store")
            .WithSummary("Refreshes the lock timestamp for a leased operational work item.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/work-items/{workItemId:long}/complete",
                async (
                    long workItemId,
                    OperationalWorkItemCompleteRequest request,
                    IOperationalWorkItemLeaseService leaseService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await leaseService.CompleteAsync(workItemId, request, cancellationToken);
                        return response.Success ? Results.Ok(response) : Results.Conflict(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("CompleteOperationalWorkItem")
            .WithTags("Operational Store")
            .WithSummary("Marks a leased operational work item completed.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/work-items/{workItemId:long}/fail",
                async (
                    long workItemId,
                    OperationalWorkItemFailRequest request,
                    IOperationalWorkItemLeaseService leaseService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await leaseService.FailAsync(workItemId, request, cancellationToken);
                        return response.Success ? Results.Ok(response) : Results.Conflict(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("FailOperationalWorkItem")
            .WithTags("Operational Store")
            .WithSummary("Marks a leased operational work item failed.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/work-items/{workItemId:long}/release",
                async (
                    long workItemId,
                    OperationalWorkItemReleaseRequest request,
                    IOperationalWorkItemRecoveryService recoveryService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await recoveryService.ReleaseAsync(workItemId, request, cancellationToken);
                        return response.Success ? Results.Ok(response) : Results.Conflict(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("ReleaseOperationalWorkItem")
            .WithTags("Operational Store")
            .WithSummary("Releases a leased operational work item back to Created.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/work-items/{workItemId:long}/reset",
                async (
                    long workItemId,
                    OperationalWorkItemResetRequest request,
                    IOperationalWorkItemRecoveryService recoveryService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await recoveryService.ResetAsync(workItemId, request, cancellationToken);
                        return response.Success ? Results.Ok(response) : Results.Conflict(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("ResetOperationalWorkItem")
            .WithTags("Operational Store")
            .WithSummary("Resets an operational work item to Created for recovery/testing.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return api;
    }
}
