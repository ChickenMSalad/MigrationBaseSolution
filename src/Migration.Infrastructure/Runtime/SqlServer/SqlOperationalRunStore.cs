using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Infrastructure.Runtime.SqlServer;

public sealed record SqlOperationalRunSummary(
    Guid RunId,
    string RunName,
    string? EnvironmentName,
    string? SourceSystem,
    string? TargetSystem,
    string Status,
    bool IsDryRun,
    DateTime RequestedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    int TotalWorkItems,
    int QueuedCount,
    int RetryScheduledCount,
    int RunningCount,
    int CompletedCount,
    int FailedCount,
    DateTime? LastWorkItemUpdatedAtUtc);

public sealed class SqlOperationalRunStore
{
    private readonly ISqlOperationalConnectionFactory _connectionFactory;

    public SqlOperationalRunStore(ISqlOperationalConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<int> EnqueueManifestWorkItemsAsync(
        Guid runId,
        string workType,
        int batchSize,
        int maxAttempts,
        int priority,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workType))
        {
            throw new ArgumentException("WorkType is required.", nameof(workType));
        }

        await using DbConnection connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "migration.usp_EnqueueManifestWorkItems";
        AddParameter(command, "@RunId", DbType.Guid, runId);
        AddParameter(command, "@WorkType", DbType.String, workType);
        AddParameter(command, "@BatchSize", DbType.Int32, batchSize <= 0 ? 5000 : batchSize);
        AddParameter(command, "@MaxAttempts", DbType.Int32, maxAttempts <= 0 ? 5 : maxAttempts);
        AddParameter(command, "@Priority", DbType.Int32, priority);

        object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task StartMigrationRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        return ExecuteNonQueryStoredProcedureAsync(
            "migration.usp_StartMigrationRun",
            command =>
            {
                AddParameter(command, "@RunId", DbType.Guid, runId);
                AddParameter(command, "@StartedAtUtc", DbType.DateTime2, DateTime.UtcNow);
            },
            cancellationToken);
    }

    public Task CompleteMigrationRunIfDrainedAsync(Guid runId, CancellationToken cancellationToken)
    {
        return ExecuteNonQueryStoredProcedureAsync(
            "migration.usp_CompleteMigrationRunIfDrained",
            command => AddParameter(command, "@RunId", DbType.Guid, runId),
            cancellationToken);
    }

    public async Task<SqlOperationalRunSummary?> GetRunSummaryAsync(Guid runId, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "migration.usp_GetRunOperationalSummary";
        AddParameter(command, "@RunId", DbType.Guid, runId);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadRunSummary(reader);
    }

    public async Task<IReadOnlyList<SqlOperationalRunSummary>> GetRunnableMigrationRunsAsync(
        int maxRows,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "migration.usp_GetRunnableMigrationRuns";
        AddParameter(command, "@MaxRows", DbType.Int32, maxRows <= 0 ? 25 : maxRows);

        var results = new List<SqlOperationalRunSummary>();
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new SqlOperationalRunSummary(
                GetGuid(reader, "RunId"),
                GetString(reader, "RunName"),
                GetNullableString(reader, "EnvironmentName"),
                GetNullableString(reader, "SourceSystem"),
                GetNullableString(reader, "TargetSystem"),
                GetString(reader, "Status"),
                GetBoolean(reader, "IsDryRun"),
                GetDateTime(reader, "RequestedAtUtc"),
                GetNullableDateTime(reader, "StartedAtUtc"),
                GetNullableDateTime(reader, "CompletedAtUtc"),
                0,
                0,
                0,
                0,
                0,
                0,
                null));
        }

        return results;
    }

    private static SqlOperationalRunSummary ReadRunSummary(DbDataReader reader)
    {
        return new SqlOperationalRunSummary(
            GetGuid(reader, "RunId"),
            GetString(reader, "RunName"),
            GetNullableString(reader, "EnvironmentName"),
            GetNullableString(reader, "SourceSystem"),
            GetNullableString(reader, "TargetSystem"),
            GetString(reader, "Status"),
            GetBoolean(reader, "IsDryRun"),
            GetDateTime(reader, "RequestedAtUtc"),
            GetNullableDateTime(reader, "StartedAtUtc"),
            GetNullableDateTime(reader, "CompletedAtUtc"),
            GetInt32(reader, "TotalWorkItems"),
            GetInt32(reader, "QueuedCount"),
            GetInt32(reader, "RetryScheduledCount"),
            GetInt32(reader, "RunningCount"),
            GetInt32(reader, "CompletedCount"),
            GetInt32(reader, "FailedCount"),
            GetNullableDateTime(reader, "LastWorkItemUpdatedAtUtc"));
    }

    private async Task ExecuteNonQueryStoredProcedureAsync(
        string storedProcedureName,
        Action<DbCommand> configureCommand,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = storedProcedureName;
        configureCommand(command);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddParameter(DbCommand command, string name, DbType dbType, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string GetString(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    private static string? GetNullableString(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int GetInt32(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool GetBoolean(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Guid GetGuid(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.GetGuid(ordinal);
    }

    private static DateTime GetDateTime(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.GetDateTime(ordinal);
    }

    private static DateTime? GetNullableDateTime(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
