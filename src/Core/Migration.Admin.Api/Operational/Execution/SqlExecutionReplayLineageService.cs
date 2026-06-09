using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayLineageService : IExecutionReplayLineageService
{
    private readonly IConfiguration _configuration;

    public SqlExecutionReplayLineageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ExecutionReplayLineageResult> ReadLineageAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var current = await ReadNodeAsync(connection, executionSessionId, cancellationToken);
        if (current is null)
        {
            throw new InvalidOperationException($"Execution session was not found: {executionSessionId}");
        }

        var ancestors = new List<ExecutionReplayLineageNode>();
        var cursor = current;
        var safety = 0;

        while (cursor.ReplaySourceExecutionSessionId.HasValue && safety < 10)
        {
            safety++;
            var parent = await ReadNodeAsync(connection, cursor.ReplaySourceExecutionSessionId.Value, cancellationToken);
            if (parent is null)
            {
                break;
            }

            ancestors.Add(parent);
            cursor = parent;
        }

        var rootExecutionSessionId = ancestors.Count == 0
            ? current.ExecutionSessionId
            : ancestors[^1].ExecutionSessionId;

        var children = await ReadChildrenAsync(connection, executionSessionId, cancellationToken);

        return new ExecutionReplayLineageResult(
            ExecutionSessionId: executionSessionId,
            RootExecutionSessionId: rootExecutionSessionId,
            SourceExecutionSessionId: current.ReplaySourceExecutionSessionId,
            ReplayDepth: current.ReplayDepth,
            ReplayScope: current.ReplayScope,
            Ancestors: ancestors,
            Children: children);
    }

    private static async Task<ExecutionReplayLineageNode?> ReadNodeAsync(
        SqlConnection connection,
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    ExecutionSessionId,
    ReplaySourceExecutionSessionId,
    Name,
    Status,
    ReplayScope,
    ReplayDepth,
    CreatedUtc
FROM dbo.MigrationExecutionSessions
WHERE ExecutionSessionId = @ExecutionSessionId;
";

        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ExecutionReplayLineageNode(
            ExecutionSessionId: reader.GetGuid(0),
            ReplaySourceExecutionSessionId: reader.IsDBNull(1) ? null : reader.GetGuid(1),
            Name: reader.GetString(2),
            Status: reader.GetString(3),
            ReplayScope: reader.IsDBNull(4) ? null : reader.GetString(4),
            ReplayDepth: reader.GetInt32(5),
            CreatedUtc: reader.GetFieldValue<DateTimeOffset>(6));
    }

    private static async Task<IReadOnlyList<ExecutionReplayLineageNode>> ReadChildrenAsync(
        SqlConnection connection,
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        var children = new List<ExecutionReplayLineageNode>();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    ExecutionSessionId,
    ReplaySourceExecutionSessionId,
    Name,
    Status,
    ReplayScope,
    ReplayDepth,
    CreatedUtc
FROM dbo.MigrationExecutionSessions
WHERE ReplaySourceExecutionSessionId = @ExecutionSessionId
ORDER BY CreatedUtc DESC;
";

        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            children.Add(new ExecutionReplayLineageNode(
                ExecutionSessionId: reader.GetGuid(0),
                ReplaySourceExecutionSessionId: reader.IsDBNull(1) ? null : reader.GetGuid(1),
                Name: reader.GetString(2),
                Status: reader.GetString(3),
                ReplayScope: reader.IsDBNull(4) ? null : reader.GetString(4),
                ReplayDepth: reader.GetInt32(5),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(6)));
        }

        return children;
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


