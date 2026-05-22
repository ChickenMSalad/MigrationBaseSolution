using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionSessionStore : IExecutionSessionStore
{
    private readonly IConfiguration _configuration;

    public SqlExecutionSessionStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ExecutionSessionRecord> CreateAsync(
        CreateExecutionSessionRequest request,
        CancellationToken cancellationToken)
    {
        var executionSessionId = Guid.NewGuid();
        var name = string.IsNullOrWhiteSpace(request.Name)
            ? $"Execution Session {DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}"
            : request.Name.Trim();

        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.MigrationExecutionSessions
(
    ExecutionSessionId,
    MigrationRunId,
    Name,
    SourceConnector,
    TargetConnector,
    Status,
    CreatedUtc,
    Notes
)
VALUES
(
    @ExecutionSessionId,
    @MigrationRunId,
    @Name,
    @SourceConnector,
    @TargetConnector,
    @Status,
    SYSUTCDATETIME(),
    @Notes
);
";

        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);
        command.Parameters.AddWithValue("@MigrationRunId", (object?)request.MigrationRunId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@SourceConnector", NormalizeDbValue(request.SourceConnector));
        command.Parameters.AddWithValue("@TargetConnector", NormalizeDbValue(request.TargetConnector));
        command.Parameters.AddWithValue("@Status", "created");
        command.Parameters.AddWithValue("@Notes", NormalizeDbValue(request.Notes));

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new ExecutionSessionRecord(
            ExecutionSessionId: executionSessionId,
            MigrationRunId: request.MigrationRunId,
            Name: name,
            SourceConnector: NormalizeText(request.SourceConnector),
            TargetConnector: NormalizeText(request.TargetConnector),
            Status: "created",
            CreatedUtc: DateTimeOffset.UtcNow,
            StartedUtc: null,
            CompletedUtc: null,
            Notes: NormalizeText(request.Notes));
    }

    public async Task<IReadOnlyList<ExecutionSessionRecord>> ReadRecentAsync(
        int take,
        CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 250);
        var connectionString = GetConnectionString();
        var sessions = new List<ExecutionSessionRecord>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
    ExecutionSessionId,
    MigrationRunId,
    Name,
    SourceConnector,
    TargetConnector,
    Status,
    CreatedUtc,
    StartedUtc,
    CompletedUtc,
    Notes
FROM dbo.MigrationExecutionSessions
ORDER BY CreatedUtc DESC;
";

        command.Parameters.AddWithValue("@Take", safeTake);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            sessions.Add(new ExecutionSessionRecord(
                ExecutionSessionId: reader.GetGuid(0),
                MigrationRunId: reader.IsDBNull(1) ? null : reader.GetGuid(1),
                Name: reader.GetString(2),
                SourceConnector: reader.IsDBNull(3) ? null : reader.GetString(3),
                TargetConnector: reader.IsDBNull(4) ? null : reader.GetString(4),
                Status: reader.GetString(5),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(6),
                StartedUtc: reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                CompletedUtc: reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
                Notes: reader.IsDBNull(9) ? null : reader.GetString(9)));
        }

        return sessions;
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

    private static object NormalizeDbValue(string? value)
    {
        var normalized = NormalizeText(value);
        return normalized is null ? DBNull.Value : normalized;
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
