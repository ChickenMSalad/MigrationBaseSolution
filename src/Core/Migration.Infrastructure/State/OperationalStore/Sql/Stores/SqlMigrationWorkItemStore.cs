using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Application.Models.OperationalStore.Statuses;
using Migration.Infrastructure.State.OperationalStore.Sql.Mappers;
using Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores;

public sealed class SqlMigrationWorkItemStore : IMigrationWorkItemStore
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlMigrationWorkItemStore> _logger;

    public SqlMigrationWorkItemStore(
        ISqlConnectionFactory connectionFactory,
        ILogger<SqlMigrationWorkItemStore> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<MigrationWorkItemRecord?> GetAsync(
        long workItemId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(WorkItemStoreSql.GetById, connection);

        command.Parameters.AddWithValue("@WorkItemId", workItemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MigrationWorkItemRecordMapper.Map(reader);
    }

    public async Task AddAsync(
        MigrationWorkItemRecord workItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(WorkItemStoreSql.Insert, connection);

        AddInsertParameters(command, workItem);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddBatchAsync(
        IReadOnlyCollection<MigrationWorkItemRecord> workItems,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItems);

        if (workItems.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        foreach (var workItem in workItems)
        {
            await using var command = new SqlCommand(WorkItemStoreSql.Insert, connection);

            AddInsertParameters(command, workItem);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task MarkLockedAsync(
        long workItemId,
        string lockedBy,
        DateTimeOffset lockedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(WorkItemStoreSql.MarkLocked, connection);

        command.Parameters.AddWithValue("@WorkItemId", workItemId);
        command.Parameters.AddWithValue("@LockedBy", lockedBy);
        command.Parameters.AddWithValue("@LockedAt", lockedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkCompletedAsync(
        long workItemId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(WorkItemStoreSql.MarkCompleted, connection);

        command.Parameters.AddWithValue("@WorkItemId", workItemId);
        command.Parameters.AddWithValue("@Status", MigrationWorkItemStatuses.Completed);
        command.Parameters.AddWithValue("@CompletedAt", completedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        long workItemId,
        string failureReason,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(WorkItemStoreSql.MarkFailed, connection);

        command.Parameters.AddWithValue("@WorkItemId", workItemId);
        command.Parameters.AddWithValue("@Status", MigrationWorkItemStatuses.Failed);
        command.Parameters.AddWithValue("@FailedAt", failedAt);
        command.Parameters.AddWithValue("@LastFailureReason", failureReason);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddInsertParameters(
        SqlCommand command,
        MigrationWorkItemRecord workItem)
    {
        command.Parameters.AddWithValue("@WorkItemId", workItem.WorkItemId);
        command.Parameters.AddWithValue("@RunId", workItem.RunId);
        command.Parameters.AddWithValue("@ManifestRecordId", workItem.ManifestRecordId);
        command.Parameters.AddWithValue("@Status", workItem.Status);
        command.Parameters.AddWithValue("@AttemptCount", workItem.AttemptCount);
        command.Parameters.AddWithValue("@CreatedAt", workItem.CreatedAt);
        command.Parameters.AddWithValue("@LockedAt", ToDbValue(workItem.LockedAt));
        command.Parameters.AddWithValue("@LockedBy", ToDbValue(workItem.LockedBy));
        command.Parameters.AddWithValue("@CompletedAt", ToDbValue(workItem.CompletedAt));
        command.Parameters.AddWithValue("@FailedAt", ToDbValue(workItem.FailedAt));
        command.Parameters.AddWithValue("@LastFailureReason", ToDbValue(workItem.LastFailureReason));
    }

    private static object ToDbValue(object? value)
    {
        return value ?? DBNull.Value;
    }
}
