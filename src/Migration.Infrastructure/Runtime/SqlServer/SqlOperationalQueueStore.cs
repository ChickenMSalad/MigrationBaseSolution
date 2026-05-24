using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Infrastructure.Runtime.SqlServer;

public sealed class SqlOperationalQueueStore
{
    private readonly ISqlOperationalConnectionFactory _connectionFactory;

    public SqlOperationalQueueStore(ISqlOperationalConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<SqlOperationalWorkItem>> ClaimWorkItemsAsync(
        SqlClaimWorkItemsRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.WorkerId))
        {
            throw new ArgumentException("WorkerId is required.", nameof(request));
        }

        if (request.BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "BatchSize must be greater than zero.");
        }

        await using DbConnection connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "migration.usp_ClaimWorkItems";

        AddParameter(command, "@WorkerId", DbType.String, request.WorkerId);
        AddParameter(command, "@BatchSize", DbType.Int32, request.BatchSize);
        AddParameter(command, "@LeaseSeconds", DbType.Int32, request.LeaseSeconds <= 0 ? 300 : request.LeaseSeconds);
        AddParameter(command, "@RunId", DbType.Guid, request.RunId);

        var results = new List<SqlOperationalWorkItem>();
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new SqlOperationalWorkItem(
                GetInt64(reader, "WorkItemId"),
                GetGuid(reader, "RunId"),
                GetNullableInt64(reader, "ManifestRowId"),
                GetString(reader, "WorkType"),
                GetString(reader, "Status"),
                GetInt32(reader, "AttemptCount"),
                GetInt32(reader, "MaxAttempts"),
                GetNullableString(reader, "PayloadJson"),
                GetNullableDateTime(reader, "LeaseExpiresAtUtc")));
        }

        return results;
    }

    public async Task CompleteWorkItemAsync(
        long workItemId,
        string workerId,
        string? resultJson,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryStoredProcedureAsync(
            "migration.usp_CompleteWorkItem",
            command =>
            {
                AddParameter(command, "@WorkItemId", DbType.Int64, workItemId);
                AddParameter(command, "@WorkerId", DbType.String, workerId);
                AddParameter(command, "@ResultJson", DbType.String, resultJson);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task FailWorkItemAsync(
        SqlFailWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        await ExecuteNonQueryStoredProcedureAsync(
            "migration.usp_FailWorkItem",
            command =>
            {
                AddParameter(command, "@WorkItemId", DbType.Int64, request.WorkItemId);
                AddParameter(command, "@WorkerId", DbType.String, request.WorkerId);
                AddParameter(command, "@ErrorCode", DbType.String, request.ErrorCode);
                AddParameter(command, "@ErrorMessage", DbType.String, request.ErrorMessage);
                AddParameter(command, "@ExceptionType", DbType.String, request.ExceptionType);
                AddParameter(command, "@IsRetryable", DbType.Boolean, request.IsRetryable);
                AddParameter(command, "@RetryDelaySeconds", DbType.Int32, request.RetryDelaySeconds <= 0 ? 300 : request.RetryDelaySeconds);
                AddParameter(command, "@FailureJson", DbType.String, request.FailureJson);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordHeartbeatAsync(
        string workerId,
        string? hostName,
        int? processId,
        string? runtimeVersion,
        Guid? currentRunId,
        string status,
        string? detailsJson,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryStoredProcedureAsync(
            "migration.usp_RecordWorkerHeartbeat",
            command =>
            {
                AddParameter(command, "@WorkerId", DbType.String, workerId);
                AddParameter(command, "@HostName", DbType.String, hostName);
                AddParameter(command, "@ProcessId", DbType.Int32, processId);
                AddParameter(command, "@RuntimeVersion", DbType.String, runtimeVersion);
                AddParameter(command, "@CurrentRunId", DbType.Guid, currentRunId);
                AddParameter(command, "@Status", DbType.String, string.IsNullOrWhiteSpace(status) ? "Healthy" : status);
                AddParameter(command, "@DetailsJson", DbType.String, detailsJson);
            },
            cancellationToken).ConfigureAwait(false);
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

    private static long GetInt64(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.GetInt64(ordinal);
    }

    private static long? GetNullableInt64(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static int GetInt32(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.GetInt32(ordinal);
    }

    private static Guid GetGuid(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.GetGuid(ordinal);
    }

    private static DateTime? GetNullableDateTime(DbDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
