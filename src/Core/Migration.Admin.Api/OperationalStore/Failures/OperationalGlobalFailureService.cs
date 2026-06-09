using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureService : IOperationalGlobalFailureService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalGlobalFailureService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<OperationalGlobalRecentFailuresResponse> GetRecentFailuresAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        var schema = GetSchemaName();

        var sql = $"""
            SELECT TOP (@Limit)
                f.FailureId,
                f.RunId,
                f.ManifestRecordId,
                f.WorkItemId,
                f.FailureType,
                f.Message,
                f.Details,
                f.IsRetriable,
                f.CreatedAt,
                RunStatus = r.Status,
                r.SourceSystem,
                r.TargetSystem,
                WorkItemStatus = wi.Status
            FROM [{schema}].[MigrationFailures] f
            INNER JOIN [{schema}].[Runs] r
                ON r.RunId = f.RunId
            LEFT JOIN [{schema}].[WorkItems] wi
                ON wi.WorkItemId = f.WorkItemId
            ORDER BY f.CreatedAt DESC, f.FailureId DESC;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Limit", safeLimit);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var failures = new List<OperationalGlobalFailureItem>();

        while (await reader.ReadAsync(cancellationToken))
        {
            failures.Add(new OperationalGlobalFailureItem
            {
                FailureId = reader.GetGuid(reader.GetOrdinal("FailureId")),
                RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
                ManifestRecordId = ReadNullableGuid(reader, "ManifestRecordId"),
                WorkItemId = ReadNullableGuid(reader, "WorkItemId"),
                FailureType = ReadNullableString(reader, "FailureType") ?? "Failure",
                Message = ReadNullableString(reader, "Message") ?? "Failure recorded.",
                Details = ReadNullableString(reader, "Details"),
                IsRetriable = reader.GetBoolean(reader.GetOrdinal("IsRetriable")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
                RunStatus = reader.GetString(reader.GetOrdinal("RunStatus")),
                SourceSystem = reader.GetString(reader.GetOrdinal("SourceSystem")),
                TargetSystem = reader.GetString(reader.GetOrdinal("TargetSystem")),
                WorkItemStatus = ReadNullableString(reader, "WorkItemStatus")
            });
        }

        return new OperationalGlobalRecentFailuresResponse
        {
            Count = failures.Count,
            Limit = safeLimit,
            GeneratedAt = DateTimeOffset.UtcNow,
            Failures = failures
        };
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;
    }

    private static Guid? ReadNullableGuid(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}


