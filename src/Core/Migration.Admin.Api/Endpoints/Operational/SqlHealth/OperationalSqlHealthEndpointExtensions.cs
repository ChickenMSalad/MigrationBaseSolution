using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Endpoints.Operational.SqlHealth;

public static class OperationalSqlHealthEndpointExtensions
{
    private static readonly ExpectedSqlObject[] ExpectedObjects =
    [
        new("dbo", "AdminRuns", Required: true, Purpose: "Control-plane run summary state"),
        new("migration", "Runs", Required: true, Purpose: "Operational run state"),
        new("migration", "ManifestRows", Required: true, Purpose: "Large manifest row state"),
        new("migration", "WorkItems", Required: true, Purpose: "Executable work item queue"),
        new("migration", "OperationalEvents", Required: false, Purpose: "Optional explicit operational event store"),
        new("migration", "ExecutionSessions", Required: false, Purpose: "Optional explicit execution-session store"),
        new("migration", "ExecutionWorkerHeartbeats", Required: false, Purpose: "Optional explicit worker heartbeat store")
    ];

    public static IEndpointRouteBuilder MapOperationalSqlHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/operational/sql")
            .WithTags("Operational SQL");

        group.MapGet("/health", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var response = await ProbeAsync(configuration, cancellationToken).ConfigureAwait(false);
            return Results.Ok(response);
        })
        .WithName("GetOperationalSqlHealth")
        .Produces<OperationalSqlHealthResponse>(StatusCodes.Status200OK);

        group.MapGet("/readiness", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var response = await ProbeAsync(configuration, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new OperationalSqlReadinessResponse(
                Ready: string.Equals(response.Status, "healthy", StringComparison.OrdinalIgnoreCase),
                Status: response.Status,
                DatabaseName: response.DatabaseName,
                RequiredMissingObjects: response.MissingObjects.Where(static x => x.Required).ToArray(),
                OptionalMissingObjects: response.MissingObjects.Where(static x => !x.Required).ToArray(),
                Message: response.Message));
        })
        .WithName("GetOperationalSqlReadiness")
        .Produces<OperationalSqlReadinessResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<OperationalSqlHealthResponse> ProbeAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new OperationalSqlHealthResponse(
                Status: "not-configured",
                DatabaseName: null,
                VerifiedObjects: Array.Empty<OperationalSqlObjectStatus>(),
                MissingObjects: ExpectedObjects,
                Message: "Operational SQL connection string is not configured. Checked MigrationOperationalStore, OperationalSql, DefaultConnection, and SqlOperationalStore:ConnectionString.");
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var verified = new List<OperationalSqlObjectStatus>();
            var missing = new List<ExpectedSqlObject>();

            foreach (var expected in ExpectedObjects)
            {
                if (await TableExistsAsync(connection, expected.Schema, expected.Name, cancellationToken).ConfigureAwait(false))
                {
                    var rowCount = await TryReadRowCountAsync(connection, expected.Schema, expected.Name, cancellationToken).ConfigureAwait(false);
                    verified.Add(new OperationalSqlObjectStatus(
                        Schema: expected.Schema,
                        Name: expected.Name,
                        Required: expected.Required,
                        Purpose: expected.Purpose,
                        RowCount: rowCount));
                }
                else
                {
                    missing.Add(expected);
                }
            }

            var requiredMissing = missing.Where(static x => x.Required).ToArray();
            var status = requiredMissing.Length == 0 ? "healthy" : "schema-incomplete";
            var message = requiredMissing.Length == 0
                ? "Operational SQL database is reachable and required runtime tables are present."
                : "Operational SQL database is reachable, but required runtime tables are missing.";

            return new OperationalSqlHealthResponse(
                Status: status,
                DatabaseName: connection.Database,
                VerifiedObjects: verified,
                MissingObjects: missing,
                Message: message);
        }
        catch (Exception ex) when (ex is SqlException || ex is InvalidOperationException || ex is TimeoutException)
        {
            return new OperationalSqlHealthResponse(
                Status: "unhealthy",
                DatabaseName: null,
                VerifiedObjects: Array.Empty<OperationalSqlObjectStatus>(),
                MissingObjects: ExpectedObjects,
                Message: ex.Message);
        }
    }

    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetConnectionString("MigrationOperationalStore")
            ?? configuration.GetConnectionString("OperationalSql")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:MigrationOperationalStore"]
            ?? configuration["ConnectionStrings:OperationalSql"]
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? configuration["SqlOperationalStore:ConnectionString"];
    }

    private static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(1)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = @SchemaName
  AND TABLE_NAME = @TableName;";
        command.Parameters.AddWithValue("@SchemaName", schema);
        command.Parameters.AddWithValue("@TableName", table);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result) == 1;
    }

    private static async Task<long?> TryReadRowCountAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT_BIG(1) FROM {QuoteName(schema)}.{QuoteName(table)};";
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt64(result);
        }
        catch (SqlException)
        {
            return null;
        }
    }

    private static string QuoteName(string value)
    {
        return "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }
}

public sealed record ExpectedSqlObject(
    string Schema,
    string Name,
    bool Required,
    string Purpose);

public sealed record OperationalSqlObjectStatus(
    string Schema,
    string Name,
    bool Required,
    string Purpose,
    long? RowCount);

public sealed record OperationalSqlHealthResponse(
    string Status,
    string? DatabaseName,
    IReadOnlyList<OperationalSqlObjectStatus> VerifiedObjects,
    IReadOnlyList<ExpectedSqlObject> MissingObjects,
    string Message);

public sealed record OperationalSqlReadinessResponse(
    bool Ready,
    string Status,
    string? DatabaseName,
    IReadOnlyList<ExpectedSqlObject> RequiredMissingObjects,
    IReadOnlyList<ExpectedSqlObject> OptionalMissingObjects,
    string Message);
