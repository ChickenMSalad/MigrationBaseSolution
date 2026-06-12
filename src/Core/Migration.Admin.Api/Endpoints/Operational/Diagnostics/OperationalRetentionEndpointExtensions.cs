using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRetentionEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRetentionEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/retention/status",
                async (IOperationalRetentionService retentionService, CancellationToken cancellationToken) =>
                {
                    var response = await retentionService.GetStatusAsync(cancellationToken);
                    return Results.Ok(response);
                })
            .WithName("GetOperationalRetentionStatus")
            .WithTags("Operational Store")
            .WithSummary("Gets operational retention status and eligibility counts.")
            .Produces<OperationalRetentionStatusResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapPost(
                "/operational/retention/archive-eligible",
                async (IOperationalRetentionService retentionService, CancellationToken cancellationToken) =>
                {
                    var response = await retentionService.ArchiveEligibleAsync(cancellationToken);
                    return Results.Ok(response);
                })
            .WithName("ArchiveEligibleOperationalRuns")
            .WithTags("Operational Store")
            .WithSummary("Archives eligible operational runs when retention is enabled.")
            .Produces<OperationalRetentionActionResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapPost(
                "/operational/retention/purge-archived",
                async (IOperationalRetentionService retentionService, CancellationToken cancellationToken) =>
                {
                    var response = await retentionService.PurgeArchivedAsync(cancellationToken);
                    return Results.Ok(response);
                })
            .WithName("PurgeArchivedOperationalRuns")
            .WithTags("Operational Store")
            .WithSummary("Purges archived operational runs when retention is enabled.")
            .Produces<OperationalRetentionActionResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


