using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Migration.ControlPlane.Services;

namespace Migration.Admin.Api.Endpoints;

public static class DeleteEndpointExtensions
{
    public static IEndpointRouteBuilder MapControlPlaneDeleteEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api")
            .WithTags("Delete");

        api.MapDelete("/runs/{runId}", async (
                    string runId,
                    IAdminProjectStore store,
                    IConfiguration configuration,
                    CancellationToken cancellationToken) =>
                await DeleteRunEverywhereAsync(runId, store, configuration, cancellationToken).ConfigureAwait(false))
            .WithName("DeleteRun")
            .WithSummary("Deletes an Admin run and its SQL operational runtime records by run id or run key.");

        api.MapDelete("/connectors/{connectorType}", (string connectorType) =>
            Results.Problem(
                title: "Connectors are code-registered and cannot be deleted.",
                detail: $"Connector '{connectorType}' is part of the compiled connector catalog. Disable connector availability through GenericMigrationRuntime filtering or remove it from the catalog/registration code.",
                statusCode: StatusCodes.Status405MethodNotAllowed))
            .WithName("DeleteConnector")
            .WithSummary("Returns 405 because connectors are currently compiled catalog entries, not user-created records.");

        return app;
    }

    private static async Task<IResult> DeleteRunEverywhereAsync(
        string runIdOrKey,
        IAdminProjectStore store,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runIdOrKey))
        {
            return Results.BadRequest(new { error = "Run id is required." });
        }

        var deletedAdminRun = await store.DeleteRunAsync(runIdOrKey, cancellationToken).ConfigureAwait(false);
        var runtimeResult = await DeleteRuntimeRunAsync(runIdOrKey, configuration, cancellationToken).ConfigureAwait(false);

        if (!deletedAdminRun && !string.IsNullOrWhiteSpace(runtimeResult.RunKey))
        {
            deletedAdminRun = await store.DeleteRunAsync(runtimeResult.RunKey!, cancellationToken).ConfigureAwait(false);
        }

        if (!deletedAdminRun && !string.IsNullOrWhiteSpace(runtimeResult.AdminRunId))
        {
            deletedAdminRun = await store.DeleteRunAsync(runtimeResult.AdminRunId!, cancellationToken).ConfigureAwait(false);
        }

        if (!deletedAdminRun && runtimeResult.RunId is not null)
        {
            deletedAdminRun = await store.DeleteRunAsync(runtimeResult.RunId.Value.ToString(), cancellationToken).ConfigureAwait(false);
        }

        if (!deletedAdminRun && runtimeResult.AdminRunDeleteAttempted)
        {
            deletedAdminRun = runtimeResult.AdminRunsDeleted > 0;
        }

        if (!deletedAdminRun && runtimeResult.RunsDeleted == 0 && runtimeResult.WorkItemsDeleted == 0)
        {
            return Results.NotFound(new { runId = runIdOrKey });
        }

        return Results.NoContent();
    }

    private static async Task<RuntimeDeleteResult> DeleteRuntimeRunAsync(
        string runIdOrKey,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString(configuration);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var resolved = await ResolveRuntimeRunAsync(connection, runIdOrKey, cancellationToken).ConfigureAwait(false);

        var runId = resolved.RunId;
        var runKey = resolved.RunKey;
        var adminRunId = resolved.AdminRunId;

        var workItemsDeleted = 0;
        var runsDeleted = 0;
        var adminRunsDeleted = 0;

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (runId is not null)
            {
                workItemsDeleted = await ExecuteAsync(
                    connection,
                    transaction,
                    "DELETE FROM migration.WorkItems WHERE RunId = @RunId;",
                    command => command.Parameters.Add(new SqlParameter("@RunId", SqlDbType.UniqueIdentifier) { Value = runId.Value }),
                    cancellationToken).ConfigureAwait(false);

                runsDeleted = await ExecuteAsync(
                    connection,
                    transaction,
                    "DELETE FROM migration.Runs WHERE RunId = @RunId;",
                    command => command.Parameters.Add(new SqlParameter("@RunId", SqlDbType.UniqueIdentifier) { Value = runId.Value }),
                    cancellationToken).ConfigureAwait(false);
            }

            adminRunsDeleted = await DeleteAdminRunRowsAsync(
                connection,
                transaction,
                runIdOrKey,
                runId,
                runKey,
                adminRunId,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }

        return new RuntimeDeleteResult(
            RunId: runId,
            RunKey: runKey,
            AdminRunId: adminRunId,
            WorkItemsDeleted: workItemsDeleted,
            RunsDeleted: runsDeleted,
            AdminRunsDeleted: adminRunsDeleted,
            AdminRunDeleteAttempted: true);
    }

    private static async Task<RuntimeRunIdentity> ResolveRuntimeRunAsync(
        SqlConnection connection,
        string runIdOrKey,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = @"
SELECT TOP (1)
    r.RunId,
    r.RunKey,
    a.RunId AS AdminRunId
FROM migration.Runs r
LEFT JOIN dbo.AdminRuns a
    ON a.RunId = r.RunKey
    OR a.RunId = CONVERT(nvarchar(64), r.RunId)
WHERE
    (@RunGuid IS NOT NULL AND r.RunId = @RunGuid)
    OR r.RunKey = @RunKey
    OR a.RunId = @RunKey;";

        if (Guid.TryParse(runIdOrKey, out var runGuid))
        {
            command.Parameters.Add(new SqlParameter("@RunGuid", SqlDbType.UniqueIdentifier) { Value = runGuid });
        }
        else
        {
            command.Parameters.Add(new SqlParameter("@RunGuid", SqlDbType.UniqueIdentifier) { Value = DBNull.Value });
        }

        command.Parameters.Add(new SqlParameter("@RunKey", SqlDbType.NVarChar, 256) { Value = runIdOrKey });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new RuntimeRunIdentity(
                RunId: Guid.TryParse(runIdOrKey, out var parsed) ? parsed : null,
                RunKey: null,
                AdminRunId: null);
        }

        var runId = reader["RunId"] is Guid id ? id : (Guid?)null;
        var runKey = reader["RunKey"] is DBNull ? null : Convert.ToString(reader["RunKey"]);
        var adminRunId = reader["AdminRunId"] is DBNull ? null : Convert.ToString(reader["AdminRunId"]);

        return new RuntimeRunIdentity(runId, runKey, adminRunId);
    }

    private static async Task<int> DeleteAdminRunRowsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string runIdOrKey,
        Guid? runId,
        string? runKey,
        string? adminRunId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandType = CommandType.Text;
        command.CommandText = @"
DELETE FROM dbo.AdminRuns
WHERE RunId IN (@InputRunId, @RunGuidText, @RunKey, @AdminRunId);";
        command.Parameters.Add(new SqlParameter("@InputRunId", SqlDbType.NVarChar, 256) { Value = runIdOrKey });
        command.Parameters.Add(new SqlParameter("@RunGuidText", SqlDbType.NVarChar, 64) { Value = runId is null ? DBNull.Value : runId.Value.ToString() });
        command.Parameters.Add(new SqlParameter("@RunKey", SqlDbType.NVarChar, 256) { Value = string.IsNullOrWhiteSpace(runKey) ? DBNull.Value : runKey });
        command.Parameters.Add(new SqlParameter("@AdminRunId", SqlDbType.NVarChar, 256) { Value = string.IsNullOrWhiteSpace(adminRunId) ? DBNull.Value : adminRunId });

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ExecuteAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        Action<SqlCommand> configure,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandType = CommandType.Text;
        command.CommandText = sql;
        configure(command);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("OperationalSql") ??
            configuration.GetConnectionString("MigrationOperationalStore") ??
            configuration["OperationalSql:ConnectionString"] ??
            configuration["SqlOperationalStore:ConnectionString"] ??
            configuration["ConnectionStrings:OperationalSql"] ??
            configuration["ConnectionStrings:MigrationOperationalStore"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Operational SQL connection string is not configured. Configure ConnectionStrings:OperationalSql or ConnectionStrings:MigrationOperationalStore.");
        }

        return connectionString;
    }

    private sealed record RuntimeRunIdentity(Guid? RunId, string? RunKey, string? AdminRunId);

    private sealed record RuntimeDeleteResult(
        Guid? RunId,
        string? RunKey,
        string? AdminRunId,
        int WorkItemsDeleted,
        int RunsDeleted,
        int AdminRunsDeleted,
        bool AdminRunDeleteAttempted);
}
