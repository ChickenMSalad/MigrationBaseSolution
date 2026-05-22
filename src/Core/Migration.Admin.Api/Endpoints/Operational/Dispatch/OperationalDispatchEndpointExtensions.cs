using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatchEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatchEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/dispatch/sample",
                (
                    IOperationalRunDispatchSampleRequestFactory sampleRequestFactory,
                    int? count) =>
                {
                    var request = sampleRequestFactory.CreateSample(
                        count.GetValueOrDefault(3));

                    return Results.Ok(request);
                })
            .WithName("GetOperationalRunDispatchSample")
            .WithTags("Operational Store")
            .WithSummary("Returns a sample operational run dispatch request payload.")
            .Produces<OperationalRunDispatchRequest>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        api.MapPost(
                "/operational/runs/dispatch",
                async (
                    OperationalRunDispatchRequest request,
                    IOperationalRunDispatchRequestHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await handler.HandleAsync(
                            request,
                            cancellationToken);

                        return Results.Accepted(
                            $"/api/runs/{response.RunId}",
                            response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new
                        {
                            error = ex.Message
                        });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.BadRequest(new
                        {
                            error = ex.Message
                        });
                    }
                })
            .WithName("DispatchOperationalRun")
            .WithTags("Operational Store")
            .WithSummary("Creates an operational SQL-backed run, persists manifest records, creates work items, and publishes operational queue messages.")
            .Produces<OperationalRunDispatchResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return api;
    }
}
