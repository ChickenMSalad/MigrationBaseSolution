using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalWorkItemLeaseExpirationEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalWorkItemLeaseExpirationEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/work-items/expired-leases",
                async (
                    IOperationalLeaseExpirationService leaseExpirationService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await leaseExpirationService.ListExpiredAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalExpiredWorkItemLeases")
            .WithTags("Operational Store")
            .WithSummary("Lists expired operational work item leases.")
            .Produces<OperationalExpiredLeaseListResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapPost(
                "/operational/work-items/reclaim-expired",
                async (
                    OperationalReclaimExpiredLeasesRequest request,
                    IOperationalLeaseExpirationService leaseExpirationService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await leaseExpirationService.ReclaimExpiredAsync(
                            request,
                            cancellationToken);

                        return Results.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("ReclaimOperationalExpiredWorkItemLeases")
            .WithTags("Operational Store")
            .WithSummary("Reclaims expired operational work item leases back to Created.")
            .Produces<OperationalReclaimExpiredLeasesResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return api;
    }
}


