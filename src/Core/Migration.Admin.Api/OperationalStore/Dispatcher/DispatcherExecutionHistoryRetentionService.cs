using Migration.Infrastructure.State.OperationalStore.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryRetentionService
    : IDispatcherExecutionHistoryRetentionService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;
    private readonly IOptions<DispatcherExecutionHistoryRetentionOptions> _options;
    private readonly ILogger<DispatcherExecutionHistoryRetentionService> _logger;

    public DispatcherExecutionHistoryRetentionService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions,
        IOptions<DispatcherExecutionHistoryRetentionOptions> options,
        ILogger<DispatcherExecutionHistoryRetentionService> logger)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
        _options = options;
        _logger = logger;
    }

    public async Task<DispatcherExecutionHistoryRetentionStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var options = NormalizeOptions();
        var schema = GetSchemaName();
        var purgeBefore = DateTimeOffset.UtcNow.AddDays(-options.PurgeAfterDays);

        var sql = $"""
            SELECT COUNT(1)
            FROM [{schema}].[DispatcherExecutions]
            WHERE StartedAt < @PurgeBefore;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PurgeBefore", purgeBefore);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        var eligibleCount = Convert.ToInt32(result);

        return new DispatcherExecutionHistoryRetentionStatusResponse
        {
            Enabled = options.Enabled,
            PurgeAfterDays = options.PurgeAfterDays,
            BatchSize = options.BatchSize,
            PurgeBefore = purgeBefore,
            EligiblePurgeExecutionCount = eligibleCount,
            Mode = options.Enabled ? "Enabled" : "Disabled"
        };
    }

    public async Task<DispatcherExecutionHistoryRetentionPurgeResponse> PurgeEligibleAsync(
        CancellationToken cancellationToken = default)
    {
        var options = NormalizeOptions();
        var schema = GetSchemaName();
        var purgeBefore = DateTimeOffset.UtcNow.AddDays(-options.PurgeAfterDays);

        if (!options.Enabled)
        {
            return new DispatcherExecutionHistoryRetentionPurgeResponse
            {
                Enabled = false,
                Executed = false,
                PurgedExecutionCount = 0,
                PurgeBefore = purgeBefore,
                Message = "Dispatcher execution history retention is disabled."
            };
        }

        var sql = $"""
            ;WITH EligibleExecutions AS
            (
                SELECT TOP (@BatchSize)
                    ExecutionId
                FROM [{schema}].[DispatcherExecutions] WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE StartedAt < @PurgeBefore
                ORDER BY StartedAt, ExecutionId
            )
            DELETE de
            FROM [{schema}].[DispatcherExecutions] de
            INNER JOIN EligibleExecutions e
                ON e.ExecutionId = de.ExecutionId;

            SELECT @@ROWCOUNT;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchSize", options.BatchSize);
        command.Parameters.AddWithValue("@PurgeBefore", purgeBefore);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        var purgedCount = Convert.ToInt32(result);

        if (purgedCount > 0)
        {
            _logger.LogWarning(
                "Purged {PurgedCount} dispatcher execution history record(s).",
                purgedCount);
        }

        return new DispatcherExecutionHistoryRetentionPurgeResponse
        {
            Enabled = true,
            Executed = true,
            PurgedExecutionCount = purgedCount,
            PurgeBefore = purgeBefore,
            Message = $"Purged {purgedCount} dispatcher execution history record(s)."
        };
    }

    private DispatcherExecutionHistoryRetentionOptions NormalizeOptions()
    {
        var value = _options.Value;

        return new DispatcherExecutionHistoryRetentionOptions
        {
            Enabled = value.Enabled,
            PurgeAfterDays = Math.Max(1, value.PurgeAfterDays),
            BatchSize = Math.Clamp(value.BatchSize, 1, 10000)
        };
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }
}
