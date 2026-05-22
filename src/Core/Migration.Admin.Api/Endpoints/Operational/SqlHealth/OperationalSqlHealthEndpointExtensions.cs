using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Endpoints.Operational.SqlHealth;

public static class OperationalSqlHealthEndpointExtensions
{
    private static readonly string[] ExpectedTables =
    [
        "MigrationProjects",
        "MigrationRuns",
        "MigrationManifestRows",
        "MigrationWorkItems",
        "MigrationFailures",
        "MigrationRunCheckpoints",
        "MigrationAssetMappings",
        "MigrationConnectorRegistrations"
    ];

    public static IEndpointRouteBuilder MapOperationalSqlHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/sql")
            .WithTags("Operational SQL");

        group.MapGet("/health", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var connectionString =
                configuration.GetConnectionString("OperationalSql") ??
                configuration["OperationalSql:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Results.Ok(new OperationalSqlHealthResponse(
                    Status: "not-configured",
                    DatabaseName: null,
                    VerifiedTables: Array.Empty<string>(),
                    MissingTables: ExpectedTables,
                    Message: "Operational SQL connection string is not configured."));
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                var verified = new List<string>();
                var missing = new List<string>();

                foreach (var table in ExpectedTables)
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = @"
SELECT COUNT(1)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = @TableName;
";

                    command.Parameters.AddWithValue("@TableName", table);

                    var result = await command.ExecuteScalarAsync(cancellationToken);
                    var count = Convert.ToInt32(result);

                    if (count == 1)
                    {
                        verified.Add(table);
                    }
                    else
                    {
                        missing.Add(table);
                    }
                }

                var status = missing.Count == 0 ? "healthy" : "schema-incomplete";

                return Results.Ok(new OperationalSqlHealthResponse(
                    Status: status,
                    DatabaseName: connection.Database,
                    VerifiedTables: verified,
                    MissingTables: missing,
                    Message: missing.Count == 0
                        ? "Operational SQL database is reachable and expected tables are present."
                        : "Operational SQL database is reachable, but expected tables are missing."));
            }
            catch (Exception ex)
            {
                return Results.Ok(new OperationalSqlHealthResponse(
                    Status: "unhealthy",
                    DatabaseName: null,
                    VerifiedTables: Array.Empty<string>(),
                    MissingTables: ExpectedTables,
                    Message: ex.Message));
            }
        })
        .WithName("GetOperationalSqlHealth");

        return endpoints;
    }
}

public sealed record OperationalSqlHealthResponse(
    string Status,
    string? DatabaseName,
    IReadOnlyList<string> VerifiedTables,
    IReadOnlyList<string> MissingTables,
    string Message);
