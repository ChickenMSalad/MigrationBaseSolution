$repoRoot = (Resolve-Path ".").Path
$servicePath = Join-Path $repoRoot "src\Migration.Admin.Api\OperationalStore\Runs\Health\OperationalGlobalRunHealthSummaryService.cs"

if (-not (Test-Path $servicePath)) {
    throw "Could not find $servicePath"
}

$backupPath = "$servicePath.126B.bak"
Copy-Item -Path $servicePath -Destination $backupPath -Force
Write-Host "Backed up current service to $backupPath"

@'

using Migration.Infrastructure.State.OperationalStore.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthSummaryService
    : IOperationalGlobalRunHealthSummaryService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalGlobalRunHealthSummaryService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<OperationalGlobalRunHealthSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var runStatuses = await ReadRunStatusesAsync(
            connection,
            schema,
            cancellationToken);

        var workItems = await ReadWorkItemCountsAsync(
            connection,
            schema,
            cancellationToken);

        var failureCount = await ReadFailureCountAsync(
            connection,
            schema,
            cancellationToken);

        var totalRunCount = runStatuses.Sum(s => s.Count);
        var completedRunCount = CountStatus(runStatuses, "Completed");
        var failedRunCount = CountStatus(runStatuses, "Failed");
        var cancelRequestedRunCount = CountStatus(runStatuses, "CancelRequested");
        var abortedRunCount = CountStatus(runStatuses, "Aborted");

        var activeRunCount = Math.Max(
            totalRunCount -
            completedRunCount -
            failedRunCount -
            abortedRunCount,
            0);

        var completionPercent = workItems.TotalWorkItemCount == 0
            ? 0m
            : Math.Round(
                ((decimal)workItems.CompletedWorkItemCount / workItems.TotalWorkItemCount) * 100m,
                2);

        return new OperationalGlobalRunHealthSummaryResponse
        {
            TotalRunCount = totalRunCount,
            ActiveRunCount = activeRunCount,
            CompletedRunCount = completedRunCount,
            FailedRunCount = failedRunCount,
            CancelRequestedRunCount = cancelRequestedRunCount,
            AbortedRunCount = abortedRunCount,
            TotalWorkItemCount = workItems.TotalWorkItemCount,
            OutstandingWorkItemCount = workItems.OutstandingWorkItemCount,
            LockedWorkItemCount = workItems.LockedWorkItemCount,
            CompletedWorkItemCount = workItems.CompletedWorkItemCount,
            FailedWorkItemCount = workItems.FailedWorkItemCount,
            TotalFailureCount = failureCount,
            CompletionPercent = completionPercent,
            GeneratedAt = DateTimeOffset.UtcNow,
            RunStatuses = runStatuses,
            Messages = BuildMessages(
                totalRunCount,
                activeRunCount,
                completedRunCount,
                failedRunCount,
                failureCount,
                completionPercent)
        };
    }

    private static async Task<IReadOnlyCollection<OperationalGlobalRunHealthStatusMetric>> ReadRunStatusesAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                Status,
                Count = CAST(COUNT_BIG(1) AS BIGINT)
            FROM [{schema}].[MigrationRuns]
            GROUP BY Status
            ORDER BY COUNT_BIG(1) DESC, Status;
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<OperationalGlobalRunHealthStatusMetric>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OperationalGlobalRunHealthStatusMetric
            {
                RunStatus = ReadNullableString(reader, "Status") ?? string.Empty,
                Count = ReadInt(reader, "Count")
            });
        }

        return results;
    }

    private static async Task<WorkItemCounts> ReadWorkItemCountsAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                TotalWorkItemCount = CAST(COUNT_BIG(1) AS BIGINT),
                OutstandingWorkItemCount = CAST(COALESCE(SUM(CASE WHEN Status IN (N'Created', N'Pending', N'Queued') THEN 1 ELSE 0 END), 0) AS BIGINT),
                LockedWorkItemCount = CAST(COALESCE(SUM(CASE WHEN Status = N'Locked' THEN 1 ELSE 0 END), 0) AS BIGINT),
                CompletedWorkItemCount = CAST(COALESCE(SUM(CASE WHEN Status = N'Completed' THEN 1 ELSE 0 END), 0) AS BIGINT),
                FailedWorkItemCount = CAST(COALESCE(SUM(CASE WHEN Status = N'Failed' THEN 1 ELSE 0 END), 0) AS BIGINT)
            FROM [{schema}].[MigrationWorkItems];
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new WorkItemCounts();
        }

        return new WorkItemCounts
        {
            TotalWorkItemCount = ReadInt(reader, "TotalWorkItemCount"),
            OutstandingWorkItemCount = ReadInt(reader, "OutstandingWorkItemCount"),
            LockedWorkItemCount = ReadInt(reader, "LockedWorkItemCount"),
            CompletedWorkItemCount = ReadInt(reader, "CompletedWorkItemCount"),
            FailedWorkItemCount = ReadInt(reader, "FailedWorkItemCount")
        };
    }

    private static async Task<int> ReadFailureCountAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT Count = CAST(COUNT_BIG(1) AS BIGINT)
            FROM [{schema}].[MigrationFailures];
            """;

        await using var command = new SqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value ?? 0);
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

    private static int CountStatus(
        IReadOnlyCollection<OperationalGlobalRunHealthStatusMetric> statuses,
        string status)
    {
        return statuses
            .Where(s => s.RunStatus.Equals(status, StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.Count);
    }

    private static IReadOnlyCollection<string> BuildMessages(
        int totalRunCount,
        int activeRunCount,
        int completedRunCount,
        int failedRunCount,
        int failureCount,
        decimal completionPercent)
    {
        return new[]
        {
            $"Operational store contains {totalRunCount} run(s).",
            $"{activeRunCount} run(s) are active or not finalized.",
            $"{completedRunCount} run(s) are completed.",
            $"{failedRunCount} run(s) are failed.",
            $"{failureCount} failure record(s) are present.",
            $"Overall work-item completion is {completionPercent}%."
        };
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;
    }

    private sealed class WorkItemCounts
    {
        public int TotalWorkItemCount { get; init; }

        public int OutstandingWorkItemCount { get; init; }

        public int LockedWorkItemCount { get; init; }

        public int CompletedWorkItemCount { get; init; }

        public int FailedWorkItemCount { get; init; }
    }
}

'@ | Set-Content -Path $servicePath -NoNewline

Write-Host "Restored OperationalGlobalRunHealthSummaryService.cs with clean implementation."
Write-Host ""
Write-Host "Next:"
Write-Host "  dotnet build"
Write-Host "  Restart Admin API"
Write-Host "  ./scripts/operational-global-run-health-summary-smoke-test.ps1 -BaseUrl `"https://localhost:55436`""
