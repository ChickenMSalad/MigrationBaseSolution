using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalQueueDepthAnalyticsService
    : IOperationalGlobalQueueDepthAnalyticsService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalGlobalQueueDepthAnalyticsService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<OperationalGlobalQueueDepthAnalyticsResponse> GetAnalyticsAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var statuses = await ReadStatusesAsync(connection, schema, cancellationToken);
        var total = statuses.Sum(s => s.Count);
        var outstanding = CountStatuses(statuses, "Created", "Pending", "Queued");
        var locked = CountStatuses(statuses, "Locked");
        var completed = CountStatuses(statuses, "Completed");
        var failed = CountStatuses(statuses, "Failed");

        var completionPercent = total == 0
            ? 0m
            : Math.Round(((decimal)completed / total) * 100m, 2);

        var pressureScore = CalculateQueuePressureScore(
            total,
            outstanding,
            locked,
            failed);

        return new OperationalGlobalQueueDepthAnalyticsResponse
        {
            TotalWorkItemCount = total,
            OutstandingWorkItemCount = outstanding,
            LockedWorkItemCount = locked,
            CompletedWorkItemCount = completed,
            FailedWorkItemCount = failed,
            CompletionPercent = completionPercent,
            QueuePressureScore = pressureScore,
            QueuePressureLevel = ToPressureLevel(pressureScore),
            GeneratedAt = DateTimeOffset.UtcNow,
            Statuses = statuses,
            Messages = BuildMessages(
                total,
                outstanding,
                locked,
                completed,
                failed,
                completionPercent,
                pressureScore)
        };
    }

    private static async Task<IReadOnlyCollection<OperationalGlobalQueueDepthStatusMetric>> ReadStatusesAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                Status,
                Count = CAST(COUNT_BIG(1) AS BIGINT)
            FROM [{schema}].[WorkItems]
            GROUP BY Status
            ORDER BY COUNT_BIG(1) DESC, Status;
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<OperationalGlobalQueueDepthStatusMetric>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OperationalGlobalQueueDepthStatusMetric
            {
                Status = ReadNullableString(reader, "Status") ?? string.Empty,
                Count = ReadInt(reader, "Count")
            });
        }

        return results;
    }

    private static int CountStatuses(
        IReadOnlyCollection<OperationalGlobalQueueDepthStatusMetric> statuses,
        params string[] statusNames)
    {
        return statuses
            .Where(s => statusNames.Any(name =>
                s.Status.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Sum(s => s.Count);
    }

    private static int CalculateQueuePressureScore(
        int total,
        int outstanding,
        int locked,
        int failed)
    {
        if (total == 0)
        {
            return 0;
        }

        var score = 0;
        score += Math.Min(outstanding * 2, 40);
        score += Math.Min(locked * 2, 25);
        score += Math.Min(failed * 10, 35);

        return Math.Clamp(score, 0, 100);
    }

    private static string ToPressureLevel(int score)
    {
        if (score >= 75)
        {
            return "Critical";
        }

        if (score >= 50)
        {
            return "High";
        }

        if (score >= 25)
        {
            return "Elevated";
        }

        return "Normal";
    }

    private static IReadOnlyCollection<string> BuildMessages(
        int total,
        int outstanding,
        int locked,
        int completed,
        int failed,
        decimal completionPercent,
        int pressureScore)
    {
        return new[]
        {
            $"Operational queue contains {total} work item(s).",
            $"{outstanding} work item(s) are outstanding.",
            $"{locked} work item(s) are locked.",
            $"{completed} work item(s) are completed.",
            $"{failed} work item(s) are failed.",
            $"Queue completion is {completionPercent}%.",
            $"Queue pressure score is {pressureScore}."
        };
    }

    private static int ReadInt(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;
    }
}


