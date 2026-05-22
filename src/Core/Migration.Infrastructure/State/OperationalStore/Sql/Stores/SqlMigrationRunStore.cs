using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Application.Models.OperationalStore.Statuses;
using Migration.Infrastructure.State.OperationalStore.Sql.Mappers;
using Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores;

public sealed class SqlMigrationRunStore : IMigrationRunStore
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlMigrationRunStore> _logger;

    public SqlMigrationRunStore(
        ISqlConnectionFactory connectionFactory,
        ILogger<SqlMigrationRunStore> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<MigrationRunRecord?> GetAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(RunStoreSql.GetById, connection);

        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MigrationRunRecordMapper.Map(reader);
    }

    public async Task CreateAsync(
        MigrationRunRecord run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(RunStoreSql.Insert, connection);

        command.Parameters.AddWithValue("@RunId", run.RunId);
        command.Parameters.AddWithValue("@SourceSystem", run.SourceSystem);
        command.Parameters.AddWithValue("@TargetSystem", run.TargetSystem);
        command.Parameters.AddWithValue("@Status", run.Status);
        command.Parameters.AddWithValue("@CreatedAt", run.CreatedAt);
        command.Parameters.AddWithValue("@StartedAt", ToDbValue(run.StartedAt));
        command.Parameters.AddWithValue("@CompletedAt", ToDbValue(run.CompletedAt));
        command.Parameters.AddWithValue("@FailedAt", ToDbValue(run.FailedAt));
        command.Parameters.AddWithValue("@FailureReason", ToDbValue(run.FailureReason));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkStartedAsync(
        Guid runId,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(RunStoreSql.MarkStarted, connection);

        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@Status", MigrationRunStatuses.Running);
        command.Parameters.AddWithValue("@StartedAt", startedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkCompletedAsync(
        Guid runId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(RunStoreSql.MarkCompleted, connection);

        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@Status", MigrationRunStatuses.Completed);
        command.Parameters.AddWithValue("@CompletedAt", completedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid runId,
        string failureReason,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(RunStoreSql.MarkFailed, connection);

        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@Status", MigrationRunStatuses.Failed);
        command.Parameters.AddWithValue("@FailedAt", failedAt);
        command.Parameters.AddWithValue("@FailureReason", failureReason);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static object ToDbValue(
        object? value)
    {
        return value ?? DBNull.Value;
    }
}
