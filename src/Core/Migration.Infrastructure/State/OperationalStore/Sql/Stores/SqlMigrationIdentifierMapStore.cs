using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Infrastructure.State.OperationalStore.Sql.Mappers;
using Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores;

public sealed class SqlMigrationIdentifierMapStore : IMigrationIdentifierMapStore
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlMigrationIdentifierMapStore> _logger;

    public SqlMigrationIdentifierMapStore(
        ISqlConnectionFactory connectionFactory,
        ILogger<SqlMigrationIdentifierMapStore> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<MigrationIdentifierMapRecord?> GetBySourceIdAsync(
        Guid runId,
        string sourceId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(IdentifierMapStoreSql.GetBySourceId, connection);

        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@SourceId", sourceId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MigrationIdentifierMapRecordMapper.Map(reader);
    }

    public async Task<MigrationIdentifierMapRecord?> GetByManifestRecordIdAsync(
        Guid manifestRecordId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(IdentifierMapStoreSql.GetByManifestRecordId, connection);

        command.Parameters.AddWithValue("@ManifestRecordId", manifestRecordId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MigrationIdentifierMapRecordMapper.Map(reader);
    }

    public async Task AddAsync(
        MigrationIdentifierMapRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(IdentifierMapStoreSql.Insert, connection);

        AddInsertParameters(command, record);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddBatchAsync(
        IReadOnlyCollection<MigrationIdentifierMapRecord> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        foreach (var record in records)
        {
            await using var command = new SqlCommand(IdentifierMapStoreSql.Insert, connection);

            AddInsertParameters(command, record);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void AddInsertParameters(
        SqlCommand command,
        MigrationIdentifierMapRecord record)
    {
        command.Parameters.AddWithValue("@IdentifierMapId", record.IdentifierMapId);
        command.Parameters.AddWithValue("@RunId", record.RunId);
        command.Parameters.AddWithValue("@ManifestRecordId", record.ManifestRecordId);
        command.Parameters.AddWithValue("@SourceId", record.SourceId);
        command.Parameters.AddWithValue("@TargetId", record.TargetId);
        command.Parameters.AddWithValue("@TargetPath", ToDbValue(record.TargetPath));
        command.Parameters.AddWithValue("@CreatedAt", record.CreatedAt);
    }

    private static object ToDbValue(
        object? value)
    {
        return value ?? DBNull.Value;
    }
}
