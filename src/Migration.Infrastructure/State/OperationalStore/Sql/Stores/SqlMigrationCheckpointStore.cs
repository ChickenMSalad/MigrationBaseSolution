using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Infrastructure.State.OperationalStore.Sql.Mappers;
using Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores;

public sealed class SqlMigrationCheckpointStore : IMigrationCheckpointStore
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlMigrationCheckpointStore> _logger;

    public SqlMigrationCheckpointStore(
        ISqlConnectionFactory connectionFactory,
        ILogger<SqlMigrationCheckpointStore> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<MigrationCheckpointRecord?> GetAsync(
        Guid runId,
        string checkpointName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(CheckpointStoreSql.Get, connection);

        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@CheckpointName", checkpointName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MigrationCheckpointRecordMapper.Map(reader);
    }

    public async Task<IReadOnlyList<MigrationCheckpointRecord>> GetByRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(CheckpointStoreSql.GetByRun, connection);

        command.Parameters.AddWithValue("@RunId", runId);

        var results = new List<MigrationCheckpointRecord>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MigrationCheckpointRecordMapper.Map(reader));
        }

        return results;
    }

    public async Task UpsertAsync(
        MigrationCheckpointRecord checkpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(CheckpointStoreSql.Upsert, connection);

        command.Parameters.AddWithValue("@CheckpointId", checkpoint.CheckpointId);
        command.Parameters.AddWithValue("@RunId", checkpoint.RunId);
        command.Parameters.AddWithValue("@CheckpointName", checkpoint.CheckpointName);
        command.Parameters.AddWithValue("@CheckpointValue", checkpoint.CheckpointValue);
        command.Parameters.AddWithValue("@CreatedAt", checkpoint.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", checkpoint.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
