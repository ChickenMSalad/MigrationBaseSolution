using Dapper;
using Microsoft.Extensions.Options;
using Migration.Infrastructure.Sql.Connections;
using Migration.Infrastructure.Sql.Options;
using Migration.Infrastructure.Sql.Records;

namespace Migration.Infrastructure.Sql.Stores;

public sealed class SqlOperationalBackboneStore : ISqlOperationalBackboneStore
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly SqlOperationalStoreOptions _options;

    public SqlOperationalBackboneStore(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
    }

    public async Task CreateProjectAsync(SqlMigrationProjectRecord project, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.MigrationProjects
            (
                ProjectId,
                ProjectKey,
                DisplayName,
                Status,
                CreatedAtUtc,
                UpdatedAtUtc
            )
            VALUES
            (
                @ProjectId,
                @ProjectKey,
                @DisplayName,
                @Status,
                @CreatedAtUtc,
                @UpdatedAtUtc
            );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, project, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task CreateRunAsync(SqlMigrationRunRecord run, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.MigrationRuns
            (
                RunId,
                ProjectId,
                RunKey,
                Status,
                CreatedAtUtc,
                UpdatedAtUtc,
                StartedAtUtc,
                CompletedAtUtc
            )
            VALUES
            (
                @RunId,
                @ProjectId,
                @RunKey,
                @Status,
                @CreatedAtUtc,
                @UpdatedAtUtc,
                @StartedAtUtc,
                @CompletedAtUtc
            );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, run, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<SqlMigrationRunRecord?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                RunId,
                ProjectId,
                RunKey,
                Status,
                CreatedAtUtc,
                UpdatedAtUtc,
                StartedAtUtc,
                CompletedAtUtc
            FROM dbo.MigrationRuns
            WHERE RunId = @RunId;
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<SqlMigrationRunRecord>(
            new CommandDefinition(sql, new { RunId = runId }, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpsertManifestRowsAsync(IReadOnlyCollection<SqlMigrationManifestRowRecord> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
        {
            return;
        }

        const string sql = """
            MERGE dbo.MigrationManifestRows AS target
            USING
            (
                SELECT
                    @ManifestRowId AS ManifestRowId,
                    @RunId AS RunId,
                    @RowNumber AS RowNumber,
                    @SourceIdentifier AS SourceIdentifier,
                    @SourceUri AS SourceUri,
                    @PayloadJson AS PayloadJson,
                    @Status AS Status,
                    @CreatedAtUtc AS CreatedAtUtc,
                    @UpdatedAtUtc AS UpdatedAtUtc
            ) AS source
            ON target.ManifestRowId = source.ManifestRowId
            WHEN MATCHED THEN
                UPDATE SET
                    RowNumber = source.RowNumber,
                    SourceIdentifier = source.SourceIdentifier,
                    SourceUri = source.SourceUri,
                    PayloadJson = source.PayloadJson,
                    Status = source.Status,
                    UpdatedAtUtc = source.UpdatedAtUtc
            WHEN NOT MATCHED THEN
                INSERT
                (
                    ManifestRowId,
                    RunId,
                    RowNumber,
                    SourceIdentifier,
                    SourceUri,
                    PayloadJson,
                    Status,
                    CreatedAtUtc,
                    UpdatedAtUtc
                )
                VALUES
                (
                    source.ManifestRowId,
                    source.RunId,
                    source.RowNumber,
                    source.SourceIdentifier,
                    source.SourceUri,
                    source.PayloadJson,
                    source.Status,
                    source.CreatedAtUtc,
                    source.UpdatedAtUtc
                );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, rows, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task EnqueueWorkItemsAsync(IReadOnlyCollection<SqlMigrationWorkItemRecord> workItems, CancellationToken cancellationToken = default)
    {
        if (workItems.Count == 0)
        {
            return;
        }

        const string sql = """
            INSERT INTO dbo.MigrationWorkItems
            (
                WorkItemId,
                RunId,
                ManifestRowId,
                WorkItemType,
                Status,
                AttemptCount,
                AvailableAtUtc,
                LeasedUntilUtc,
                LeaseOwner,
                PayloadJson,
                CreatedAtUtc,
                UpdatedAtUtc
            )
            VALUES
            (
                @WorkItemId,
                @RunId,
                @ManifestRowId,
                @WorkItemType,
                @Status,
                @AttemptCount,
                @AvailableAtUtc,
                @LeasedUntilUtc,
                @LeaseOwner,
                @PayloadJson,
                @CreatedAtUtc,
                @UpdatedAtUtc
            );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, workItems, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SqlMigrationWorkItemRecord>> LeaseWorkItemsAsync(Guid runId, string leaseOwner, int maxItems, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DECLARE @Now datetimeoffset = SYSUTCDATETIME();
            DECLARE @LeaseUntil datetimeoffset = DATEADD(minute, @WorkItemLeaseMinutes, @Now);

            ;WITH candidates AS
            (
                SELECT TOP (@MaxItems) *
                FROM dbo.MigrationWorkItems WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE RunId = @RunId
                  AND Status IN ('Pending', 'RetryPending')
                  AND (AvailableAtUtc IS NULL OR AvailableAtUtc <= @Now)
                  AND (LeasedUntilUtc IS NULL OR LeasedUntilUtc <= @Now)
                ORDER BY CreatedAtUtc, WorkItemId
            )
            UPDATE candidates
            SET
                Status = 'Leased',
                LeaseOwner = @LeaseOwner,
                LeasedUntilUtc = @LeaseUntil,
                AttemptCount = AttemptCount + 1,
                UpdatedAtUtc = @Now
            OUTPUT
                inserted.WorkItemId,
                inserted.RunId,
                inserted.ManifestRowId,
                inserted.WorkItemType,
                inserted.Status,
                inserted.AttemptCount,
                inserted.AvailableAtUtc,
                inserted.LeasedUntilUtc,
                inserted.LeaseOwner,
                inserted.PayloadJson,
                inserted.CreatedAtUtc,
                inserted.UpdatedAtUtc;
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<SqlMigrationWorkItemRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    RunId = runId,
                    LeaseOwner = leaseOwner,
                    MaxItems = maxItems,
                    _options.WorkItemLeaseMinutes
                },
                commandTimeout: _options.CommandTimeoutSeconds,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.AsList();
    }

    public async Task CompleteWorkItemAsync(long workItemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.MigrationWorkItems
            SET
                Status = 'Completed',
                LeasedUntilUtc = NULL,
                LeaseOwner = NULL,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE WorkItemId = @WorkItemId;
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { WorkItemId = workItemId }, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task FailWorkItemAsync(long workItemId, SqlMigrationFailureRecord failure, CancellationToken cancellationToken = default)
    {
        const string updateSql = """
            UPDATE dbo.MigrationWorkItems
            SET
                Status = 'Failed',
                LeasedUntilUtc = NULL,
                LeaseOwner = NULL,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE WorkItemId = @WorkItemId;
            """;

        const string insertSql = """
            INSERT INTO dbo.MigrationFailures
            (
                FailureId,
                RunId,
                WorkItemId,
                ManifestRowId,
                FailureType,
                FailureCode,
                Message,
                DetailsJson,
                CreatedAtUtc
            )
            VALUES
            (
                @FailureId,
                @RunId,
                @WorkItemId,
                @ManifestRowId,
                @FailureType,
                @FailureCode,
                @Message,
                @DetailsJson,
                @CreatedAtUtc
            );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(updateSql, new { WorkItemId = workItemId }, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(insertSql, failure, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task SaveCheckpointAsync(SqlMigrationCheckpointRecord checkpoint, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.MigrationRunCheckpoints
            (
                CheckpointId,
                RunId,
                CheckpointName,
                CheckpointValue,
                PayloadJson,
                CreatedAtUtc
            )
            VALUES
            (
                @CheckpointId,
                @RunId,
                @CheckpointName,
                @CheckpointValue,
                @PayloadJson,
                @CreatedAtUtc
            );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, checkpoint, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpsertAssetMappingAsync(SqlMigrationAssetMappingRecord mapping, CancellationToken cancellationToken = default)
    {
        const string sql = """
            MERGE dbo.MigrationAssetMappings AS target
            USING
            (
                SELECT
                    @AssetMappingId AS AssetMappingId,
                    @RunId AS RunId,
                    @SourceSystem AS SourceSystem,
                    @SourceIdentifier AS SourceIdentifier,
                    @TargetSystem AS TargetSystem,
                    @TargetIdentifier AS TargetIdentifier,
                    @PayloadJson AS PayloadJson,
                    @CreatedAtUtc AS CreatedAtUtc,
                    @UpdatedAtUtc AS UpdatedAtUtc
            ) AS source
            ON target.RunId = source.RunId
               AND target.SourceSystem = source.SourceSystem
               AND target.SourceIdentifier = source.SourceIdentifier
               AND target.TargetSystem = source.TargetSystem
            WHEN MATCHED THEN
                UPDATE SET
                    TargetIdentifier = source.TargetIdentifier,
                    PayloadJson = source.PayloadJson,
                    UpdatedAtUtc = source.UpdatedAtUtc
            WHEN NOT MATCHED THEN
                INSERT
                (
                    AssetMappingId,
                    RunId,
                    SourceSystem,
                    SourceIdentifier,
                    TargetSystem,
                    TargetIdentifier,
                    PayloadJson,
                    CreatedAtUtc,
                    UpdatedAtUtc
                )
                VALUES
                (
                    source.AssetMappingId,
                    source.RunId,
                    source.SourceSystem,
                    source.SourceIdentifier,
                    source.TargetSystem,
                    source.TargetIdentifier,
                    source.PayloadJson,
                    source.CreatedAtUtc,
                    source.UpdatedAtUtc
                );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, mapping, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
