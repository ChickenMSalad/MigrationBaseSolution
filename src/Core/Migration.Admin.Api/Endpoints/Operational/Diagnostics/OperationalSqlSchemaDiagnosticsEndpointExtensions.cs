using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalSqlSchemaDiagnosticsEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalSqlSchemaDiagnosticsEndpoints(
        this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(
                "/operational/sql/schema/smoke-test",
                async (
                    IOperationalSqlSchemaSmokeTestService smokeTestService,
                    CancellationToken cancellationToken) =>
                {
                    var response =
                        await smokeTestService.ExecuteAsync(cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalSqlSchemaSmokeTest")
            .WithTags("Operational Store")
            .WithSummary("Executes a smoke test against the SQL operational schema.")
            .Produces<OperationalSqlSchemaSmokeTestResult>(StatusCodes.Status200OK)
            .WithOpenApi();

        return app;
    }
}


