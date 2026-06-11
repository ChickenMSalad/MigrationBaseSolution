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

    public async Task CreateProjectAsync(
        SqlMigrationProjectRecord project,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            MERGE migration.MigrationProjects AS target
            USING
            (
                SELECT
                    @ProjectId AS ProjectId,
                    @ProjectKey AS ProjectKey,
                    @ProjectName AS ProjectName,
                    @Status AS Status,
                    @SettingsJson AS SettingsJson,
                    @CreatedAtUtc AS CreatedAtUtc,
                    @UpdatedAtUtc AS UpdatedAtUtc
            ) AS source
            ON target.ProjectId = source.ProjectId
            WHEN MATCHED THEN
                UPDATE SET
                    ProjectKey = source.ProjectKey,
                    ProjectName = source.ProjectName,
                    Status = source.Status,
                    SettingsJson = source.SettingsJson,
                    UpdatedAtUtc = source.UpdatedAtUtc
            WHEN NOT MATCHED THEN
                INSERT
                (
                    ProjectId,
                    ProjectKey,
                    ProjectName,
                    Status,
                    SettingsJson,
                    CreatedAtUtc,
                    UpdatedAtUtc
                )
                VALUES
                (
                    source.ProjectId,
                    source.ProjectKey,
                    source.ProjectName,
                    source.Status,
                    source.SettingsJson,
                    source.CreatedAtUtc,
                    source.UpdatedAtUtc
                );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, project, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task CreateRunAsync(SqlMigrationRunRecord run, CancellationToken cancellationToken = default)
    {
        const string sql = """
            MERGE migration.Runs AS target
            USING
            (
                SELECT
                    @RunId AS RunId,
                    @ProjectId AS ProjectId,
                    @RunKey AS RunKey,
                    @RunName AS RunName,
                    @SourceSystem AS SourceSystem,
                    @TargetSystem AS TargetSystem,
                    @Status AS Status,
                    @StatusReason AS StatusReason,
                    @EnvironmentName AS EnvironmentName,
                    @IsDryRun AS IsDryRun,
                    @CoordinatorOwner AS CoordinatorOwner,
                    @CoordinationLeaseExpiresUtc AS CoordinationLeaseExpiresUtc,
                    @RequestedAtUtc AS RequestedAtUtc,
                    @StartedAtUtc AS StartedAtUtc,
                    @CompletedAtUtc AS CompletedAtUtc,
                    @CreatedAtUtc AS CreatedAtUtc,
                    @UpdatedAtUtc AS UpdatedAtUtc
            ) AS source
            ON target.RunId = source.RunId
            WHEN MATCHED THEN
                UPDATE SET
                    ProjectId = source.ProjectId,
                    RunKey = source.RunKey,
                    RunName = source.RunName,
                    SourceSystem = source.SourceSystem,
                    TargetSystem = source.TargetSystem,
                    Status = source.Status,
                    StatusReason = source.StatusReason,
                    EnvironmentName = source.EnvironmentName,
                    IsDryRun = source.IsDryRun,
                    CoordinatorOwner = source.CoordinatorOwner,
                    CoordinationLeaseExpiresUtc = source.CoordinationLeaseExpiresUtc,
                    RequestedAtUtc = source.RequestedAtUtc,
                    StartedAtUtc = source.StartedAtUtc,
                    CompletedAtUtc = source.CompletedAtUtc,
                    UpdatedAtUtc = source.UpdatedAtUtc
            WHEN NOT MATCHED THEN
                INSERT
                (
                    RunId,
                    ProjectId,
                    RunKey,
                    RunName,
                    SourceSystem,
                    TargetSystem,
                    Status,
                    StatusReason,
                    EnvironmentName,
                    IsDryRun,
                    CoordinatorOwner,
                    CoordinationLeaseExpiresUtc,
                    RequestedAtUtc,
                    StartedAtUtc,
                    CompletedAtUtc,
                    CreatedAtUtc,
                    UpdatedAtUtc
                )
                VALUES
                (
                    source.RunId,
                    source.ProjectId,
                    source.RunKey,
                    source.RunName,
                    source.SourceSystem,
                    source.TargetSystem,
                    source.Status,
                    source.StatusReason,
                    source.EnvironmentName,
                    source.IsDryRun,
                    source.CoordinatorOwner,
                    source.CoordinationLeaseExpiresUtc,
                    source.RequestedAtUtc,
                    source.StartedAtUtc,
                    source.CompletedAtUtc,
                    source.CreatedAtUtc,
                    source.UpdatedAtUtc
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
                RunName,
                SourceSystem,
                TargetSystem,
                Status,
                StatusReason,
                EnvironmentName,
                IsDryRun,
                CoordinatorOwner,
                CoordinationLeaseExpiresUtc,
                RequestedAtUtc,
                StartedAtUtc,
                CompletedAtUtc,
                CreatedAtUtc,
                UpdatedAtUtc
            FROM migration.Runs
            WHERE RunId = @RunId;
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<SqlMigrationRunRecord>(
            new CommandDefinition(sql, new { RunId = runId }, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpsertManifestRowsAsync(
        IReadOnlyCollection<SqlMigrationManifestRowRecord> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
        {
            return;
        }

        const string sql = """
            MERGE migration.ManifestRows AS target
            USING
            (
                SELECT
                    @RunId AS RunId,
                    @SourceRowNumber AS SourceRowNumber,
                    @SourceExternalId AS SourceExternalId,
                    @SourcePath AS SourcePath,
                    @ContentHash AS ContentHash,
                    @Operation AS Operation,
                    @ManifestStatus AS ManifestStatus,
                    @PayloadJson AS PayloadJson,
                    @ValidationJson AS ValidationJson,
                    @CreatedAtUtc AS CreatedAtUtc,
                    @UpdatedAtUtc AS UpdatedAtUtc
            ) AS source
            ON target.RunId = source.RunId
               AND target.SourceRowNumber = source.SourceRowNumber
            WHEN MATCHED THEN
                UPDATE SET
                    SourceExternalId = source.SourceExternalId,
                    SourcePath = source.SourcePath,
                    ContentHash = source.ContentHash,
                    Operation = source.Operation,
                    ManifestStatus = source.ManifestStatus,
                    PayloadJson = source.PayloadJson,
                    ValidationJson = source.ValidationJson,
                    UpdatedAtUtc = source.UpdatedAtUtc
            WHEN NOT MATCHED THEN
                INSERT
                (
                    RunId,
                    SourceRowNumber,
                    SourceExternalId,
                    SourcePath,
                    ContentHash,
                    Operation,
                    ManifestStatus,
                    PayloadJson,
                    ValidationJson,
                    CreatedAtUtc,
                    UpdatedAtUtc
                )
                VALUES
                (
                    source.RunId,
                    source.SourceRowNumber,
                    source.SourceExternalId,
                    source.SourcePath,
                    source.ContentHash,
                    source.Operation,
                    source.ManifestStatus,
                    source.PayloadJson,
                    source.ValidationJson,
                    source.CreatedAtUtc,
                    source.UpdatedAtUtc
                );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, rows, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task EnqueueWorkItemsAsync(
        IReadOnlyCollection<SqlMigrationWorkItemRecord> workItems,
        CancellationToken cancellationToken = default)
    {
        if (workItems.Count == 0)
        {
            return;
        }

        const string sql = """
            INSERT INTO migration.WorkItems
            (
                RunId,
                ManifestRowId,
                WorkType,
                Status,
                Priority,
                AttemptCount,
                MaxAttempts,
                AvailableAtUtc,
                ClaimedBy,
                ClaimedAtUtc,
                LeaseExpiresAtUtc,
                StartedAtUtc,
                CompletedAtUtc,
                IdempotencyKey,
                PayloadJson,
                ResultJson,
                LastErrorCode,
                LastErrorMessage,
                CreatedAtUtc,
                UpdatedAtUtc,
                PartitionKey,
                NotBeforeUtc,
                LeaseExpiresUtc,
                CreatedUtc,
                LeaseOwner,
                UpdatedUtc,
                WorkItemType,
                DispatchedAtUtc
            )
            VALUES
            (
                @RunId,
                @ManifestRowId,
                @WorkType,
                @Status,
                @Priority,
                @AttemptCount,
                @MaxAttempts,
                @AvailableAtUtc,
                @ClaimedBy,
                @ClaimedAtUtc,
                @LeaseExpiresAtUtc,
                @StartedAtUtc,
                @CompletedAtUtc,
                @IdempotencyKey,
                @PayloadJson,
                @ResultJson,
                @LastErrorCode,
                @LastErrorMessage,
                @CreatedAtUtc,
                @UpdatedAtUtc,
                @PartitionKey,
                @NotBeforeUtc,
                @LeaseExpiresUtc,
                @CreatedUtc,
                @LeaseOwner,
                @UpdatedUtc,
                @WorkItemType,
                @DispatchedAtUtc
            );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, workItems, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SqlMigrationWorkItemRecord>> LeaseWorkItemsAsync(
        Guid runId,
        string leaseOwner,
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DECLARE @Now datetime2 = SYSUTCDATETIME();
            DECLARE @LeaseUntil datetime2 = DATEADD(minute, @WorkItemLeaseMinutes, @Now);

            ;WITH candidates AS
            (
                SELECT TOP (@MaxItems) *
                FROM migration.WorkItems WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE RunId = @RunId
                  AND Status IN ('Queued', 'Pending', 'RetryPending', 'FailedRetryable')
                  AND (AvailableAtUtc IS NULL OR AvailableAtUtc <= @Now)
                  AND (LeaseExpiresAtUtc IS NULL OR LeaseExpiresAtUtc <= @Now)
                ORDER BY Priority DESC, CreatedAtUtc, WorkItemId
            )
            UPDATE candidates
            SET
                Status = 'Leased',
                ClaimedBy = @LeaseOwner,
                ClaimedAtUtc = @Now,
                LeaseExpiresAtUtc = @LeaseUntil,
                LeaseOwner = @LeaseOwner,
                LeaseExpiresUtc = @LeaseUntil,
                AttemptCount = AttemptCount + 1,
                UpdatedAtUtc = @Now,
                UpdatedUtc = @Now
            OUTPUT
                inserted.WorkItemId,
                inserted.RunId,
                inserted.ManifestRowId,
                inserted.WorkType,
                inserted.Status,
                inserted.Priority,
                inserted.AttemptCount,
                inserted.MaxAttempts,
                inserted.AvailableAtUtc,
                inserted.ClaimedBy,
                inserted.ClaimedAtUtc,
                inserted.LeaseExpiresAtUtc,
                inserted.StartedAtUtc,
                inserted.CompletedAtUtc,
                inserted.IdempotencyKey,
                inserted.PayloadJson,
                inserted.ResultJson,
                inserted.LastErrorCode,
                inserted.LastErrorMessage,
                inserted.CreatedAtUtc,
                inserted.UpdatedAtUtc,
                inserted.PartitionKey,
                inserted.NotBeforeUtc,
                inserted.LeaseExpiresUtc,
                inserted.CreatedUtc,
                inserted.LeaseOwner,
                inserted.UpdatedUtc,
                inserted.WorkItemType,
                inserted.DispatchedAtUtc;
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
            UPDATE migration.WorkItems
            SET
                Status = 'Completed',
                ClaimedBy = NULL,
                ClaimedAtUtc = NULL,
                LeaseExpiresAtUtc = NULL,
                LeaseOwner = NULL,
                LeaseExpiresUtc = NULL,
                CompletedAtUtc = SYSUTCDATETIME(),
                UpdatedAtUtc = SYSUTCDATETIME(),
                UpdatedUtc = SYSUTCDATETIME()
            WHERE WorkItemId = @WorkItemId;
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { WorkItemId = workItemId }, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task FailWorkItemAsync(
        long workItemId,
        SqlMigrationFailureRecord failure,
        CancellationToken cancellationToken = default)
    {
        const string updateSql = """
            UPDATE migration.WorkItems
            SET
                Status = 'Failed',
                ClaimedBy = NULL,
                ClaimedAtUtc = NULL,
                LeaseExpiresAtUtc = NULL,
                LeaseOwner = NULL,
                LeaseExpiresUtc = NULL,
                LastErrorCode = @FailureCode,
                LastErrorMessage = @Message,
                UpdatedAtUtc = SYSUTCDATETIME(),
                UpdatedUtc = SYSUTCDATETIME()
            WHERE WorkItemId = @WorkItemId;
            """;

        const string insertSql = """
            INSERT INTO migration.WorkItemFailures
            (
                WorkItemId,
                RunId,
                AttemptNumber,
                ErrorCode,
                ErrorMessage,
                ExceptionType,
                IsRetryable,
                FailureJson,
                CreatedAtUtc
            )
            VALUES
            (
                @WorkItemId,
                @RunId,
                1,
                @FailureCode,
                @Message,
                @FailureType,
                CAST(0 AS bit),
                @DetailsJson,
                @CreatedAtUtc
            );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new
                {
                    WorkItemId = workItemId,
                    failure.FailureCode,
                    failure.Message
                },
                commandTimeout: _options.CommandTimeoutSeconds,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertSql,
                new
                {
                    WorkItemId = workItemId,
                    failure.RunId,
                    failure.FailureCode,
                    failure.Message,
                    failure.FailureType,
                    failure.DetailsJson,
                    failure.CreatedAtUtc
                },
                commandTimeout: _options.CommandTimeoutSeconds,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task SaveCheckpointAsync(SqlMigrationCheckpointRecord checkpoint, CancellationToken cancellationToken = default)
    {
        const string sql = """
            MERGE migration.ExecutionCheckpoints AS target
            USING
            (
                SELECT
                    @RunId AS RunId,
                    @CheckpointName AS CheckpointName,
                    @CheckpointValue AS CheckpointValue,
                    @PayloadJson AS CheckpointJson,
                    SYSUTCDATETIME() AS UpdatedAtUtc
            ) AS source
            ON target.RunId = source.RunId
               AND target.CheckpointName = source.CheckpointName
            WHEN MATCHED THEN
                UPDATE SET
                    CheckpointValue = source.CheckpointValue,
                    CheckpointJson = source.CheckpointJson,
                    UpdatedAtUtc = source.UpdatedAtUtc
            WHEN NOT MATCHED THEN
                INSERT
                (
                    RunId,
                    CheckpointName,
                    CheckpointValue,
                    CheckpointJson,
                    UpdatedAtUtc
                )
                VALUES
                (
                    source.RunId,
                    source.CheckpointName,
                    source.CheckpointValue,
                    source.CheckpointJson,
                    source.UpdatedAtUtc
                );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, checkpoint, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpsertAssetMappingAsync(SqlMigrationAssetMappingRecord mapping, CancellationToken cancellationToken = default)
    {
        const string sql = """
            MERGE migration.IdentifierMappings AS target
            USING
            (
                SELECT
                    @RunId AS RunId,
                    @SourceSystem AS SourceSystem,
                    @TargetSystem AS TargetSystem,
                    @SourceIdentifier AS SourceIdentifier,
                    @TargetIdentifier AS TargetIdentifier,
                    CAST(NULL AS nvarchar(200)) AS EntityType,
                    @PayloadJson AS MappingJson,
                    SYSUTCDATETIME() AS CreatedAtUtc,
                    SYSUTCDATETIME() AS UpdatedAtUtc
            ) AS source
            ON target.RunId = source.RunId
               AND target.SourceSystem = source.SourceSystem
               AND target.SourceIdentifier = source.SourceIdentifier
               AND target.TargetSystem = source.TargetSystem
            WHEN MATCHED THEN
                UPDATE SET
                    TargetIdentifier = source.TargetIdentifier,
                    MappingJson = source.MappingJson,
                    UpdatedAtUtc = source.UpdatedAtUtc
            WHEN NOT MATCHED THEN
                INSERT
                (
                    RunId,
                    SourceSystem,
                    TargetSystem,
                    SourceIdentifier,
                    TargetIdentifier,
                    EntityType,
                    MappingJson,
                    CreatedAtUtc,
                    UpdatedAtUtc
                )
                VALUES
                (
                    source.RunId,
                    source.SourceSystem,
                    source.TargetSystem,
                    source.SourceIdentifier,
                    source.TargetIdentifier,
                    source.EntityType,
                    source.MappingJson,
                    source.CreatedAtUtc,
                    source.UpdatedAtUtc
                );
            """;

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, mapping, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}