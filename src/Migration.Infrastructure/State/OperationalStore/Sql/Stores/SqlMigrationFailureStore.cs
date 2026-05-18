using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Infrastructure.State.OperationalStore.Sql.Mappers;
using Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores;

public sealed class SqlMigrationFailureStore : IMigrationFailureStore
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlMigrationFailureStore> _logger;

    public SqlMigrationFailureStore(
        ISqlConnectionFactory connectionFactory,
        ILogger<SqlMigrationFailureStore> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task AddAsync(
        MigrationFailureRecord failure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(failure);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(FailureStoreSql.Insert, connection);

        command.Parameters.AddWithValue("@FailureId", failure.FailureId);
        command.Parameters.AddWithValue("@RunId", failure.RunId);
        command.Parameters.AddWithValue("@ManifestRecordId", ToDbValue(failure.ManifestRecordId));
        command.Parameters.AddWithValue("@WorkItemId", ToDbValue(failure.WorkItemId));
        command.Parameters.AddWithValue("@FailureType", failure.FailureType);
        command.Parameters.AddWithValue("@Message", failure.Message);
        command.Parameters.AddWithValue("@Details", ToDbValue(failure.Details));
        command.Parameters.AddWithValue("@IsRetriable", failure.IsRetriable);
        command.Parameters.AddWithValue("@CreatedAt", failure.CreatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MigrationFailureRecord>> GetByRunAsync(
        Guid runId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(FailureStoreSql.GetByRun, connection);

        command.Parameters.AddWithValue("@RunId", runId);
        command.Parameters.AddWithValue("@Skip", skip);
        command.Parameters.AddWithValue("@Take", take);

        return await ReadListAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<MigrationFailureRecord>> GetByManifestRecordAsync(
        Guid manifestRecordId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(FailureStoreSql.GetByManifestRecord, connection);

        command.Parameters.AddWithValue("@ManifestRecordId", manifestRecordId);

        return await ReadListAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<MigrationFailureRecord>> GetByWorkItemAsync(
        Guid workItemId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(FailureStoreSql.GetByWorkItem, connection);

        command.Parameters.AddWithValue("@WorkItemId", workItemId);

        return await ReadListAsync(command, cancellationToken);
    }

    private static async Task<IReadOnlyList<MigrationFailureRecord>> ReadListAsync(
        SqlCommand command,
        CancellationToken cancellationToken)
    {
        var results = new List<MigrationFailureRecord>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MigrationFailureRecordMapper.Map(reader));
        }

        return results;
    }

    private static object ToDbValue(
        object? value)
    {
        return value ?? DBNull.Value;
    }
}
