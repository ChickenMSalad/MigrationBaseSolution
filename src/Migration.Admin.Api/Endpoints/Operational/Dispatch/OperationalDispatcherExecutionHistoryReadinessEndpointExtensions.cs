using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatcherExecutionHistoryReadinessEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatcherExecutionHistoryReadinessEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/dispatcher/executions/readiness",
                async (
                    IDispatcherExecutionHistoryReadinessService readinessService,
                    IServiceProvider serviceProvider,
                    CancellationToken cancellationToken) =>
                {
                    var response = await readinessService.CheckAsync(cancellationToken);

                    var historyServiceRegistered =
                        serviceProvider.GetService<IDispatcherExecutionHistoryService>() is not null;

                    if (!historyServiceRegistered)
                    {
                        response = new DispatcherExecutionHistoryReadinessResponse
                        {
                            Ready = false,
                            ServiceRegistered = false,
                            TableExists = response.TableExists,
                            RequiredColumnsExist = response.RequiredColumnsExist,
                            SchemaName = response.SchemaName,
                            MissingColumns = response.MissingColumns,
                            Messages = response.Messages
                                .Concat(new[] { "IDispatcherExecutionHistoryService is not registered." })
                                .ToArray()
                        };
                    }

                    return Results.Ok(response);
                })
            .WithName("GetOperationalDispatcherExecutionHistoryReadiness")
            .WithTags("Operational Store")
            .WithSummary("Checks dispatcher execution history readiness.")
            .Produces<DispatcherExecutionHistoryReadinessResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
