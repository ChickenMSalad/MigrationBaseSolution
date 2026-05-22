using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Operational.Events;

public sealed class SqlOperationalEventStore : IOperationalEventStore
{
    private readonly IConfiguration _configuration;

    public SqlOperationalEventStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<Guid> WriteAsync(
        string eventType,
        string severity,
        string category,
        string source,
        string message,
        string? payloadJson,
        CancellationToken cancellationToken)
    {
        return WriteAsync(
            eventType,
            severity,
            category,
            source,
            message,
            payloadJson,
            executionSessionId: null,
            migrationRunId: null,
            cancellationToken);
    }

    public async Task<Guid> WriteAsync(
        string eventType,
        string severity,
        string category,
        string source,
        string message,
        string? payloadJson,
        Guid? executionSessionId,
        Guid? migrationRunId,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();
        var eventId = Guid.NewGuid();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.MigrationOperationalEvents
(
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
)
VALUES
(
    @OperationalEventId,
    @EventType,
    @Severity,
    @Category,
    @Source,
    @Message,
    @PayloadJson,
    SYSUTCDATETIME(),
    @ExecutionSessionId,
    @MigrationRunId
);
";

        command.Parameters.AddWithValue("@OperationalEventId", eventId);
        command.Parameters.AddWithValue("@EventType", eventType);
        command.Parameters.AddWithValue("@Severity", severity);
        command.Parameters.AddWithValue("@Category", category);
        command.Parameters.AddWithValue("@Source", source);
        command.Parameters.AddWithValue("@Message", message);
        command.Parameters.AddWithValue("@PayloadJson", (object?)payloadJson ?? DBNull.Value);
        command.Parameters.AddWithValue("@ExecutionSessionId", (object?)executionSessionId ?? DBNull.Value);
        command.Parameters.AddWithValue("@MigrationRunId", (object?)migrationRunId ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return eventId;
    }

    public async Task<IReadOnlyList<OperationalEventRecord>> ReadRecentAsync(
        int take,
        CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 250);
        var connectionString = GetConnectionString();
        var events = new List<OperationalEventRecord>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
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
ORDER BY CreatedUtc DESC;
";

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
