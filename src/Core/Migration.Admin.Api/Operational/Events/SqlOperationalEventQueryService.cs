using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Operational.Events;

public sealed class SqlOperationalEventQueryService : IOperationalEventQueryService
{
    private readonly IConfiguration _configuration;

    public SqlOperationalEventQueryService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<OperationalEventRecord>> QueryAsync(
        OperationalEventQueryRequest request,
        CancellationToken cancellationToken)
    {
        var safeSkip = Math.Max(0, request.Skip);
        var safeTake = Math.Clamp(request.Take, 1, 250);

        var connectionString = GetConnectionString();
        var events = new List<OperationalEventRecord>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = @"
SELECT
    OperationalEventId,
    EventType,
    Severity,
    Category,
    Source,
    Message,
    PayloadJson,
    CreatedUtc,
    ExecutionSessionId,
    MigrationRunId
FROM dbo.MigrationOperationalEvents
WHERE
    (@Severity IS NULL OR Severity = @Severity)
    AND (@Category IS NULL OR Category = @Category)
    AND (@EventType IS NULL OR EventType = @EventType)
    AND (@FromUtc IS NULL OR CreatedUtc >= @FromUtc)
    AND (@ToUtc IS NULL OR CreatedUtc <= @ToUtc)
    AND (@ExecutionSessionId IS NULL OR ExecutionSessionId = @ExecutionSessionId)
    AND (@MigrationRunId IS NULL OR MigrationRunId = @MigrationRunId)
ORDER BY CreatedUtc DESC
OFFSET @Skip ROWS
FETCH NEXT @Take ROWS ONLY;
";

        AddNullableString(command, "@Severity", request.Severity);
        AddNullableString(command, "@Category", request.Category);
        AddNullableString(command, "@EventType", request.EventType);
        AddNullableDateTimeOffset(command, "@FromUtc", request.FromUtc);
        AddNullableDateTimeOffset(command, "@ToUtc", request.ToUtc);
        AddNullableGuid(command, "@ExecutionSessionId", request.ExecutionSessionId);
        AddNullableGuid(command, "@MigrationRunId", request.MigrationRunId);
        command.Parameters.AddWithValue("@Skip", safeSkip);
        command.Parameters.AddWithValue("@Take", safeTake);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(new OperationalEventRecord(
                OperationalEventId: reader.GetGuid(0),
                EventType: reader.GetString(1),
                Severity: reader.GetString(2),
                Category: reader.GetString(3),
                Source: reader.GetString(4),
                Message: reader.GetString(5),
                PayloadJson: reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(7),
                ExecutionSessionId: reader.IsDBNull(8) ? null : reader.GetGuid(8),
                MigrationRunId: reader.IsDBNull(9) ? null : reader.GetGuid(9)));
        }

        return events;
    }

    public async Task<OperationalEventAggregateSummary> ReadAggregateSummaryAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var totalEvents = await ReadTotalAsync(connection, fromUtc, toUtc, cancellationToken);
        var bySeverity = await ReadBucketsAsync(connection, "Severity", fromUtc, toUtc, cancellationToken);
        var byCategory = await ReadBucketsAsync(connection, "Category", fromUtc, toUtc, cancellationToken);
        var byEventType = await ReadBucketsAsync(connection, "EventType", fromUtc, toUtc, cancellationToken);

        return new OperationalEventAggregateSummary(
            FromUtc: fromUtc,
            ToUtc: toUtc,
            TotalEvents: totalEvents,
            BySeverity: bySeverity,
            ByCategory: byCategory,
            ByEventType: byEventType);
    }

    private static async Task<int> ReadTotalAsync(
        SqlConnection connection,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(1)
FROM dbo.MigrationOperationalEvents
WHERE
    (@FromUtc IS NULL OR CreatedUtc >= @FromUtc)
    AND (@ToUtc IS NULL OR CreatedUtc <= @ToUtc);
";

        AddNullableDateTimeOffset(command, "@FromUtc", fromUtc);
        AddNullableDateTimeOffset(command, "@ToUtc", toUtc);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task<IReadOnlyList<OperationalEventAggregateBucket>> ReadBucketsAsync(
        SqlConnection connection,
        string columnName,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        if (columnName is not ("Severity" or "Category" or "EventType"))
        {
            throw new InvalidOperationException($"Unsupported aggregate column: {columnName}");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT TOP (25)
    {columnName},
    COUNT(1)
FROM dbo.MigrationOperationalEvents
WHERE
    (@FromUtc IS NULL OR CreatedUtc >= @FromUtc)
    AND (@ToUtc IS NULL OR CreatedUtc <= @ToUtc)
GROUP BY {columnName}
ORDER BY COUNT(1) DESC, {columnName} ASC;
";

        AddNullableDateTimeOffset(command, "@FromUtc", fromUtc);
        AddNullableDateTimeOffset(command, "@ToUtc", toUtc);

        var buckets = new List<OperationalEventAggregateBucket>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            buckets.Add(new OperationalEventAggregateBucket(
                Name: reader.GetString(0),
                Count: reader.GetInt32(1)));
        }

        return buckets;
    }

    private static void AddNullableString(SqlCommand command, string name, string? value)
    {
        command.Parameters.AddWithValue(name, string.IsNullOrWhiteSpace(value) ? DBNull.Value : value);
    }

    private static void AddNullableDateTimeOffset(SqlCommand command, string name, DateTimeOffset? value)
    {
        command.Parameters.AddWithValue(name, value.HasValue ? value.Value : DBNull.Value);
    }

    private static void AddNullableGuid(SqlCommand command, string name, Guid? value)
    {
        command.Parameters.AddWithValue(name, value.HasValue ? value.Value : DBNull.Value);
    }

    private string GetConnectionString()
    {
        var connectionString =
            _configuration.GetConnectionString("OperationalSql") ??
            _configuration["OperationalSql:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Operational SQL connection string is not configured.");
        }

        return connectionString;
    }
}
