using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.Runs;
using Migration.Application.Operational.WorkItems;

namespace Migration.Infrastructure.Sql.Operational.Runs;

public sealed class SqlOperationalRunCoordinator : IOperationalRunCoordinator
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<SqlOperationalRunCoordinatorOptions> _options;
    private readonly IOperationalWorkItemQueue _workItemQueue;

    public SqlOperationalRunCoordinator(
        IConfiguration configuration,
        IOptions<SqlOperationalRunCoordinatorOptions> options,
        IOperationalWorkItemQueue workItemQueue)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
    }

    public async Task<IReadOnlyList<Guid>> GetRunnableRunIdsAsync(
        int maxRuns,
        CancellationToken cancellationToken = default)
    {
        var sql = $@"
select top (@MaxRuns) RunId
from {RunsTableName}
where Status in ('Queued', 'Running', 'Pending')
order by RequestedAtUtc asc, CreatedAtUtc asc;";

        await using var connection = OpenConnection();

        var rows = await connection.QueryAsync<Guid>(new CommandDefinition(
            sql,
            new { MaxRuns = Math.Clamp(maxRuns, 1, 100) },
            cancellationToken: cancellationToken));

        return rows.AsList();
    }

    public async Task<OperationalRunCoordinatorRunSnapshot?> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        return await connection.QuerySingleOrDefaultAsync<OperationalRunCoordinatorRunSnapshot>(new CommandDefinition($@"
select RunId,
       ProjectId,
       RunKey,
       Status,
       StatusReason,
       CoordinatorOwner,
       CoordinationLeaseExpiresUtc,
       StartedAtUtc,
       CompletedAtUtc,
       RequestedCancellationUtc,
       CancellationReason,
       CreatedAtUtc,
       UpdatedAtUtc
from {RunsTableName}
where RunId = @RunId;", new { RunId = runId }, cancellationToken: cancellationToken));
    }

    public async Task<OperationalRunStartResult> StartRunAsync(
        StartOperationalRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTimeOffset.UtcNow;
        var batchSize = ClampBatchSize(request.BatchSize);
        var leaseExpiresUtc = now.AddSeconds(Math.Clamp(_options.Value.CoordinationLeaseSeconds, 60, 3600));

        await using var connection = OpenConnection();
        var updated = await connection.ExecuteAsync(new CommandDefinition($@"
update {RunsTableName}
set Status = 'Running',
    StatusReason = null,
    CoordinatorOwner = @CoordinatorOwner,
    CoordinationLeaseExpiresUtc = @CoordinationLeaseExpiresUtc,
    StartedAtUtc = coalesce(StartedAtUtc, @StartedAtUtc),
    UpdatedAtUtc = @UpdatedAtUtc
where RunId = @RunId
  and Status in ('Created', 'Pending', 'Ready', 'Paused', 'RetryRequested', 'Running');", new
        {
            request.RunId,
            CoordinatorOwner = request.CoordinatorId,
            CoordinationLeaseExpiresUtc = leaseExpiresUtc,
            StartedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken: cancellationToken));

        if (updated == 0)
        {
            var existing = await GetRunAsync(request.RunId, cancellationToken);
            throw new InvalidOperationException(existing is null
                ? $"Run '{request.RunId}' was not found."
                : $"Run '{request.RunId}' cannot be started from status '{existing.Status}'.");
        }

        var manifestRows = await connection.QueryAsync<ManifestRowFanOutRow>(new CommandDefinition($@"
select top (@BatchSize) ManifestRowId,
       PayloadJson
from {ManifestRowsTableName}
where RunId = @RunId
  and Status in ('Pending', 'Ready', 'Validated')
order by RowNumber asc;", new
        {
            request.RunId,
            BatchSize = batchSize
        }, cancellationToken: cancellationToken));

        var selected = manifestRows.AsList();
        var enqueued = 0;
        foreach (var row in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payloadJson = BuildPayloadJson(request.PayloadTemplateJson, row.PayloadJson);
            await _workItemQueue.EnqueueAsync(new EnqueueOperationalWorkItemRequest(
                request.RunId,
                row.ManifestRowId,
                string.IsNullOrWhiteSpace(request.WorkItemType) ? "AssetMigration" : request.WorkItemType,
                request.PartitionKey,
                request.Priority,
                payloadJson,
                null), cancellationToken);
            enqueued++;
        }

        return new OperationalRunStartResult(
            request.RunId,
            "Running",
            enqueued,
            selected.Count,
            now,
            request.CoordinatorId);
    }

    public async Task<OperationalRunCancellationResult> RequestCancellationAsync(
        RequestOperationalRunCancellation request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTimeOffset.UtcNow;
        await using var connection = OpenConnection();
        var updated = await connection.ExecuteAsync(new CommandDefinition($@"
update {RunsTableName}
set Status = case when Status in ('Completed', 'Failed', 'Canceled') then Status else 'CancelRequested' end,
    StatusReason = @Reason,
    RequestedCancellationUtc = coalesce(RequestedCancellationUtc, @RequestedUtc),
    CancellationReason = @Reason,
    UpdatedAtUtc = @RequestedUtc
where RunId = @RunId;", new
        {
            request.RunId,
            Reason = request.Reason,
            RequestedUtc = now
        }, cancellationToken: cancellationToken));

        if (updated == 0)
        {
            throw new InvalidOperationException($"Run '{request.RunId}' was not found.");
        }

        var run = await GetRunAsync(request.RunId, cancellationToken);
        return new OperationalRunCancellationResult(
            request.RunId,
            run?.Status ?? "CancelRequested",
            request.RequestedBy,
            request.Reason,
            now);
    }

    public async Task<OperationalRunCompletionEvaluationResult> EvaluateCompletionAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var run = await GetRunAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"Run '{runId}' was not found.");

        var summary = await _workItemQueue.GetRunSummaryAsync(runId, cancellationToken);
        var nextStatus = run.Status;
        var isTerminal = false;
        string? message = null;

        if (run.Status == "CancelRequested")
        {
            nextStatus = "Canceled";
            isTerminal = true;
            message = "Run cancellation was requested.";
        }
        else if (summary.TotalCount == 0)
        {
            message = "No work items have been enqueued for this run.";
        }
        else if (summary.PendingCount == 0 && summary.LeasedCount == 0 && summary.RetryableFailedCount == 0)
        {
            if (summary.FailedCount > 0)
            {
                nextStatus = "Failed";
                message = "Run has terminal failed work items.";
            }
            else
            {
                nextStatus = "Completed";
                message = "All work items completed.";
            }

            isTerminal = true;
        }
        else
        {
            nextStatus = "Running";
            message = "Run still has active, pending, or retryable work.";
        }

        if (!string.Equals(nextStatus, run.Status, StringComparison.OrdinalIgnoreCase))
        {
            await using var connection = OpenConnection();
            await connection.ExecuteAsync(new CommandDefinition($@"
update {RunsTableName}
set Status = @Status,
    StatusReason = @StatusReason,
    CompletedAtUtc = case when @IsTerminal = 1 then coalesce(CompletedAtUtc, @EvaluatedUtc) else CompletedAtUtc end,
    CompletionEvaluatedUtc = @EvaluatedUtc,
    UpdatedAtUtc = @EvaluatedUtc
where RunId = @RunId;", new
            {
                RunId = runId,
                Status = nextStatus,
                StatusReason = message,
                IsTerminal = isTerminal ? 1 : 0,
                EvaluatedUtc = now
            }, cancellationToken: cancellationToken));
        }

        return new OperationalRunCompletionEvaluationResult(
            runId,
            run.Status,
            nextStatus,
            isTerminal,
            summary,
            now,
            message);
    }

    private int ClampBatchSize(int requestedBatchSize)
    {
        var options = _options.Value;
        var requested = requestedBatchSize <= 0 ? options.DefaultFanOutBatchSize : requestedBatchSize;
        return Math.Clamp(requested, 1, Math.Max(1, options.MaxFanOutBatchSize));
    }

    private static string? BuildPayloadJson(string? templateJson, string? manifestPayloadJson)
    {
        if (string.IsNullOrWhiteSpace(templateJson))
        {
            return manifestPayloadJson;
        }

        return templateJson.Replace("{{manifestPayloadJson}}", manifestPayloadJson ?? "null", StringComparison.OrdinalIgnoreCase);
    }

    private SqlConnection OpenConnection()
    {
        var connectionString = ResolveConnectionString();
        var connection = new SqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    private string ResolveConnectionString()
    {
        var options = _options.Value;
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return options.ConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(options.ConnectionStringName))
        {
            var named = _configuration.GetConnectionString(options.ConnectionStringName);
            if (!string.IsNullOrWhiteSpace(named))
            {
                return named;
            }
        }

        var fallback = _configuration["SqlOperationalStore:ConnectionString"]
            ?? _configuration["OperationalStore:Sql:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        throw new InvalidOperationException(
            "SQL operational run coordinator connection string is not configured. Configure ConnectionStrings:MigrationOperationalStore or SqlOperationalRunCoordinator:ConnectionString.");
    }

    private string RunsTableName => $"[{_options.Value.SchemaName}].[{_options.Value.RunsTableName}]";

    private string ManifestRowsTableName => $"[{_options.Value.SchemaName}].[{_options.Value.ManifestRowsTableName}]";

    private sealed record ManifestRowFanOutRow(long ManifestRowId, string? PayloadJson);
}
