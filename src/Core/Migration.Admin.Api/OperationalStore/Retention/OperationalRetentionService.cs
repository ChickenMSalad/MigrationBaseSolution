using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRetentionService : IOperationalRetentionService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;
    private readonly IOptions<OperationalRetentionOptions> _retentionOptions;
    private readonly ILogger<OperationalRetentionService> _logger;

    public OperationalRetentionService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions,
        IOptions<OperationalRetentionOptions> retentionOptions,
        ILogger<OperationalRetentionService> logger)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
        _retentionOptions = retentionOptions;
        _logger = logger;
    }

    public async Task<OperationalRetentionStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var options = NormalizeOptions();
        var schema = GetSchemaName();
        var archiveBefore = DateTimeOffset.UtcNow.AddDays(-options.ArchiveAfterDays);
        var purgeBefore = DateTimeOffset.UtcNow.AddDays(-options.PurgeAfterDays);

        var sql = $"""
            SELECT
                EligibleArchiveRunCount = SUM(CASE
                    WHEN Status IN (N'Completed', N'Failed', N'Aborted', N'Canceled')
                     AND CreatedAt < @ArchiveBefore
                    THEN 1 ELSE 0 END),
                EligiblePurgeRunCount = SUM(CASE
                    WHEN Status = N'Archived'
                     AND CompletedAt IS NOT NULL
                     AND CompletedAt < @PurgeBefore
                    THEN 1 ELSE 0 END)
            FROM [{schema}].[MigrationRuns];
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ArchiveBefore", archiveBefore);
        command.Parameters.AddWithValue("@PurgeBefore", purgeBefore);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var eligibleArchive = 0;
        var eligiblePurge = 0;

        if (await reader.ReadAsync(cancellationToken))
        {
            eligibleArchive = ReadInt32(reader, "EligibleArchiveRunCount");
            eligiblePurge = ReadInt32(reader, "EligiblePurgeRunCount");
        }

        return new OperationalRetentionStatusResponse
        {
            Enabled = options.Enabled,
            ArchiveAfterDays = options.ArchiveAfterDays,
            PurgeAfterDays = options.PurgeAfterDays,
            BatchSize = options.BatchSize,
            ArchiveBefore = archiveBefore,
            PurgeBefore = purgeBefore,
            EligibleArchiveRunCount = eligibleArchive,
            EligiblePurgeRunCount = eligiblePurge,
            Mode = options.Enabled ? "Enabled" : "Disabled"
        };
    }

    public async Task<OperationalRetentionActionResponse> ArchiveEligibleAsync(
        CancellationToken cancellationToken = default)
    {
        var options = NormalizeOptions();
        var archiveBefore = DateTimeOffset.UtcNow.AddDays(-options.ArchiveAfterDays);

        if (!options.Enabled)
        {
            return Disabled("ArchiveEligible", archiveBefore);
        }

        var schema = GetSchemaName();
        var sql = $"""
            DECLARE @Archived TABLE (RunId UNIQUEIDENTIFIER NOT NULL);

            ;WITH EligibleRuns AS
            (
                SELECT TOP (@BatchSize) RunId
                FROM [{schema}].[MigrationRuns] WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status IN (N'Completed', N'Failed', N'Aborted', N'Canceled')
                  AND CreatedAt < @ArchiveBefore
                ORDER BY CreatedAt, RunId
            )
            UPDATE r
                SET Status = N'Archived'
                OUTPUT inserted.RunId INTO @Archived
            FROM [{schema}].[MigrationRuns] r
            INNER JOIN EligibleRuns e ON e.RunId = r.RunId;

            SELECT RunId FROM @Archived ORDER BY RunId;
            """;

        var runIds = await ExecuteRunIdActionAsync(sql, options.BatchSize, "@ArchiveBefore", archiveBefore, cancellationToken);

        _logger.LogWarning("Archived {Count} operational run(s) by retention policy.", runIds.Count);

        return new OperationalRetentionActionResponse
        {
            Enabled = true,
            Executed = true,
            Action = "ArchiveEligible",
            AffectedRunCount = runIds.Count,
            Threshold = archiveBefore,
            RunIds = runIds,
            Message = $"Archived {runIds.Count} eligible operational run(s)."
        };
    }

    public async Task<OperationalRetentionActionResponse> PurgeArchivedAsync(
        CancellationToken cancellationToken = default)
    {
        var options = NormalizeOptions();
        var purgeBefore = DateTimeOffset.UtcNow.AddDays(-options.PurgeAfterDays);

        if (!options.Enabled)
        {
            return Disabled("PurgeArchived", purgeBefore);
        }

        var schema = GetSchemaName();
        var sql = $"""
            DECLARE @Purged TABLE (RunId UNIQUEIDENTIFIER NOT NULL);

            ;WITH EligibleRuns AS
            (
                SELECT TOP (@BatchSize) RunId
                FROM [{schema}].[MigrationRuns] WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = N'Archived'
                  AND CompletedAt IS NOT NULL
                  AND CompletedAt < @PurgeBefore
                ORDER BY CompletedAt, RunId
            )
            DELETE c FROM [{schema}].[MigrationCheckpoints] c INNER JOIN EligibleRuns e ON e.RunId = c.RunId;
            DELETE f FROM [{schema}].[MigrationFailures] f INNER JOIN EligibleRuns e ON e.RunId = f.RunId;
            DELETE w FROM [{schema}].[MigrationWorkItems] w INNER JOIN EligibleRuns e ON e.RunId = w.RunId;
            DELETE m FROM [{schema}].[MigrationManifestRecords] m INNER JOIN EligibleRuns e ON e.RunId = m.RunId;
            DELETE r
                OUTPUT deleted.RunId INTO @Purged
            FROM [{schema}].[MigrationRuns] r
            INNER JOIN EligibleRuns e ON e.RunId = r.RunId;

            SELECT RunId FROM @Purged ORDER BY RunId;
            """;

        var runIds = await ExecuteRunIdActionAsync(sql, options.BatchSize, "@PurgeBefore", purgeBefore, cancellationToken);

        _logger.LogWarning("Purged {Count} archived operational run(s) by retention policy.", runIds.Count);

        return new OperationalRetentionActionResponse
        {
            Enabled = true,
            Executed = true,
            Action = "PurgeArchived",
            AffectedRunCount = runIds.Count,
            Threshold = purgeBefore,
            RunIds = runIds,
            Message = $"Purged {runIds.Count} archived operational run(s)."
        };
    }

    private OperationalRetentionActionResponse Disabled(string action, DateTimeOffset threshold)
    {
        return new OperationalRetentionActionResponse
        {
            Enabled = false,
            Executed = false,
            Action = action,
            AffectedRunCount = 0,
            Threshold = threshold,
            Message = "Operational retention is disabled."
        };
    }

    private async Task<IReadOnlyCollection<Guid>> ExecuteRunIdActionAsync(
        string sql,
        int batchSize,
        string thresholdParameterName,
        DateTimeOffset threshold,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue(thresholdParameterName, threshold);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var runIds = new List<Guid>();

        while (await reader.ReadAsync(cancellationToken))
        {
            runIds.Add(reader.GetGuid(reader.GetOrdinal("RunId")));
        }

        return runIds;
    }

    private OperationalRetentionOptions NormalizeOptions()
    {
        var value = _retentionOptions.Value;
        return new OperationalRetentionOptions
        {
            Enabled = value.Enabled,
            ArchiveAfterDays = Math.Max(1, value.ArchiveAfterDays),
            PurgeAfterDays = Math.Max(1, value.PurgeAfterDays),
            BatchSize = Math.Clamp(value.BatchSize, 1, 500)
        };
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName) ? "migration" : _sqlOptions.Value.SchemaName;
    }

    private static int ReadInt32(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }
}
