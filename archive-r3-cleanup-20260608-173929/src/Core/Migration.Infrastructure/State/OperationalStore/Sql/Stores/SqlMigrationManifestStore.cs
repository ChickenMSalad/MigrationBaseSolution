using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Infrastructure.State.OperationalStore.Sql.Mappers;
using Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores;

public sealed class SqlMigrationManifestStore : IMigrationManifestStore
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlMigrationManifestStore> _logger;

    public SqlMigrationManifestStore(
        ISqlConnectionFactory connectionFactory,
        ILogger<SqlMigrationManifestStore> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<MigrationManifestRecord?> GetAsync(
        long manifestRecordId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(ManifestStoreSql.GetById, connection);

        command.Parameters.AddWithValue("@ManifestRecordId", manifestRecordId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MigrationManifestRecordMapper.Map(reader);
    }

    public async Task<IReadOnlyList<MigrationManifestRecord>> GetByRunAsync(
        Guid runId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(ManifestStoreSql.GetByRun, connection);

        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@Skip", skip);
        command.Parameters.AddWithValue("@Take", take);

        var records = new List<MigrationManifestRecord>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(MigrationManifestRecordMapper.Map(reader));
        }

        return records;
    }

    public async Task AddAsync(
        MigrationManifestRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(ManifestStoreSql.Insert, connection);

        AddInsertParameters(command, record);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddBatchAsync(
        IReadOnlyCollection<MigrationManifestRecord> records,
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
            await using var command = new SqlCommand(ManifestStoreSql.Insert, connection);

            AddInsertParameters(command, record);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task UpdateStatusAsync(
        long manifestRecordId,
        string status,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(ManifestStoreSql.UpdateStatus, connection);

        command.Parameters.AddWithValue("@ManifestRecordId", manifestRecordId);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@UpdatedAt", updatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddInsertParameters(
        SqlCommand command,
        MigrationManifestRecord record)
    {
        command.Parameters.AddWithValue("@ManifestRecordId", record.ManifestRecordId);
        command.Parameters.AddWithValue("@RunId", record.RunId);
        command.Parameters.AddWithValue("@SequenceNumber", record.SequenceNumber);
        command.Parameters.AddWithValue("@SourceId", record.SourceId);
        command.Parameters.AddWithValue("@SourcePath", ToDbValue(record.SourcePath));
        command.Parameters.AddWithValue("@SourceName", ToDbValue(record.SourceName));
        command.Parameters.AddWithValue("@ContentType", ToDbValue(record.ContentType));
        command.Parameters.AddWithValue("@ContentLength", ToDbValue(record.ContentLength));
        command.Parameters.AddWithValue("@Status", record.Status);
        command.Parameters.AddWithValue("@CreatedAt", record.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", ToDbValue(record.UpdatedAt));
    }

    private static object ToDbValue(
        object? value)
    {
        return value ?? DBNull.Value;
    }
}
