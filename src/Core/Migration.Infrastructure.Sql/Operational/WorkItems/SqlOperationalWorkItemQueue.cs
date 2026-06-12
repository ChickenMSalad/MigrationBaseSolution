using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.WorkItems;

namespace Migration.Infrastructure.Sql.Operational.WorkItems;

public sealed class SqlOperationalWorkItemQueue : IOperationalWorkItemQueue
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<SqlOperationalWorkItemQueueOptions> _options;

    public SqlOperationalWorkItemQueue(
        IConfiguration configuration,
        IOptions<SqlOperationalWorkItemQueueOptions> options)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<OperationalWorkItemRecord> EnqueueAsync(
        EnqueueOperationalWorkItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTimeOffset.UtcNow;

        var sql = $@"
insert into {TableName} (
    RunId, ManifestRowId, WorkItemType, Status, PartitionKey, Priority,
    AttemptCount, MaxAttempts, LeaseOwner, LeaseExpiresUtc, NotBeforeUtc,
    PayloadJson, ResultJson, LastErrorCode, LastErrorMessage, CreatedUtc, UpdatedUtc)
output inserted.WorkItemId
values (
    @RunId, @ManifestRowId, @WorkItemType, 'Pending', @PartitionKey, @Priority,
    0, @MaxAttempts, null, null, @NotBeforeUtc,
    @PayloadJson, null, null, null, @CreatedUtc, @UpdatedUtc);";

        await using var connection = OpenConnection();

        var workItemId = await connection.QuerySingleAsync<long>(
            new CommandDefinition(sql, new
            {
                request.RunId,
                request.ManifestRowId,
                request.WorkItemType,
                request.PartitionKey,
                request.Priority,
                MaxAttempts = _options.Value.DefaultMaxAttempts,
                request.NotBeforeUtc,
                request.PayloadJson,
                CreatedUtc = now,
                UpdatedUtc = now
            }, cancellationToken: cancellationToken));

        var created = await GetAsync(workItemId, cancellationToken);
        return created ?? throw new InvalidOperationException($"Work item '{workItemId}' was inserted but could not be read back.");
    }

    public async Task<IReadOnlyList<OperationalWorkItemRecord>> ClaimAsync(
        ClaimOperationalWorkItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var maxItems = Math.Clamp(request.MaxItems, 1, 500);
        var leaseSeconds = Math.Clamp(request.LeaseSeconds, 30, 3600);
        var now = DateTime.UtcNow;
        var leaseExpiresUtc = now.AddSeconds(leaseSeconds);

        var sql = $@"
;with Claimable as (
    select top (@MaxItems) *
    from {TableName} with (rowlock, readpast, updlock)
    where RunId = @RunId
      and Status in ('Pending', 'FailedRetryable')
      and (@PartitionKey is null or PartitionKey = @PartitionKey)
      and (NotBeforeUtc is null or NotBeforeUtc <= @NowUtc)
      and (LeaseExpiresUtc is null or LeaseExpiresUtc <= @NowUtc)
      and AttemptCount < MaxAttempts
    order by Priority desc, CreatedUtc asc
)
update Claimable
set Status = 'Leased',
    StartedAtUtc = coalesce(StartedAtUtc, @NowUtc),
    LeaseOwner = @WorkerId,
    LeaseExpiresUtc = @LeaseExpiresUtc,
    AttemptCount = AttemptCount + 1,
    UpdatedUtc = @NowUtc
output inserted.WorkItemId,
       inserted.RunId,
       inserted.ManifestRowId,
       inserted.WorkItemType,
       inserted.Status,
       inserted.PartitionKey,
       inserted.Priority,
       inserted.AttemptCount,
       inserted.MaxAttempts,
       inserted.LeaseOwner,
       inserted.LeaseExpiresUtc,
       inserted.NotBeforeUtc,
       inserted.PayloadJson,
       inserted.ResultJson,
       inserted.LastErrorCode,
       inserted.LastErrorMessage,
        inserted.StartedAtUtc,
        inserted.CompletedAtUtc,
       inserted.CreatedUtc,
       inserted.UpdatedUtc;";

        await using var connection = OpenConnection();
        var rows = await connection.QueryAsync<OperationalWorkItemRecord>(new CommandDefinition(sql, new
        {
            request.RunId,
            request.WorkerId,
            MaxItems = maxItems,
            LeaseExpiresUtc = leaseExpiresUtc,
            NowUtc = now,
            request.PartitionKey
        }, cancellationToken: cancellationToken));

        return rows.AsList();
    }

    public async Task<OperationalWorkItemRecord?> GetAsync(long workItemId, CancellationToken cancellationToken = default)
    {
        var sql = $@"
select WorkItemId, RunId, ManifestRowId, WorkItemType, Status, PartitionKey, Priority,
       AttemptCount, MaxAttempts, LeaseOwner, LeaseExpiresUtc, NotBeforeUtc,
       PayloadJson, ResultJson, LastErrorCode, LastErrorMessage,
       StartedAtUtc, CompletedAtUtc,
       CreatedUtc, UpdatedUtc
from {TableName}
where WorkItemId = @WorkItemId;";

        await using var connection = OpenConnection();
        return await connection.QuerySingleOrDefaultAsync<OperationalWorkItemRecord>(new CommandDefinition(sql, new
        {
            WorkItemId = workItemId
        }, cancellationToken: cancellationToken));
    }

    public async Task<OperationalWorkItemRunSummary> GetRunSummaryAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var sql = $@"
select
    @RunId as RunId,
    sum(case when Status = 'Pending' then 1 else 0 end) as PendingCount,
    sum(case when Status = 'Leased' then 1 else 0 end) as LeasedCount,
    sum(case when Status = 'Completed' then 1 else 0 end) as CompletedCount,
    sum(case when Status = 'Failed' then 1 else 0 end) as FailedCount,
    sum(case when Status = 'FailedRetryable' then 1 else 0 end) as RetryableFailedCount,
    count(1) as TotalCount,
    min(case when Status in ('Pending', 'FailedRetryable') then CreatedUtc else null end) as OldestPendingUtc,
    max(UpdatedUtc) as NewestUpdateUtc
from {TableName}
where RunId = @RunId;";

        await using var connection = OpenConnection();
        return await connection.QuerySingleAsync<OperationalWorkItemRunSummary>(new CommandDefinition(sql, new
        {
            RunId = runId
        }, cancellationToken: cancellationToken));
    }

    public async Task CompleteAsync(CompleteOperationalWorkItemRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sql = $@"
update {TableName}
set Status = 'Completed',
    ResultJson = @ResultJson,
    LeaseOwner = null,
    LeaseExpiresUtc = null,
    CompletedAtUtc = SYSUTCDATETIME(),
    UpdatedUtc = @UpdatedUtc
where WorkItemId = @WorkItemId;";

        await ExecuteStateChangeAsync(sql, request, cancellationToken);
    }

    public async Task FailAsync(FailOperationalWorkItemRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sql = $@"
update {TableName}
set Status = case when @IsRetryable = 1 and AttemptCount < MaxAttempts then 'FailedRetryable' else 'Failed' end,
    LastErrorCode = @ErrorCode,
    LastErrorMessage = @ErrorMessage,
    LeaseOwner = null,
    LeaseExpiresUtc = null,
    NotBeforeUtc = @NextAttemptUtc,
    CompletedAtUtc = case when @IsRetryable = 1 and AttemptCount < MaxAttempts then CompletedAtUtc else SYSUTCDATETIME() end,
    UpdatedUtc = @UpdatedUtc
where WorkItemId = @WorkItemId;";

        await ExecuteStateChangeAsync(sql, request, cancellationToken);
    }

    public async Task ReleaseAsync(ReleaseOperationalWorkItemRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sql = $@"
update {TableName}
set Status = 'Pending',
    LeaseOwner = null,
    LeaseExpiresUtc = null,
    NotBeforeUtc = @NextAttemptUtc,
    UpdatedUtc = @UpdatedUtc
where WorkItemId = @WorkItemId;";

        await ExecuteStateChangeAsync(sql, request, cancellationToken);
    }

    private async Task ExecuteStateChangeAsync(string sql, object request, CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        var parameters = new DynamicParameters(request);
        parameters.Add("UpdatedUtc", DateTimeOffset.UtcNow);
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        if (affected == 0)
        {
            throw new InvalidOperationException("No work item was updated. The item may not exist or the lease owner did not match.");
        }
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
            "SQL operational work item queue connection string is not configured. Configure ConnectionStrings:MigrationOperationalStore or SqlOperationalWorkItemQueue:ConnectionString.");
    }

    private string TableName
    {
        get
        {
            var options = _options.Value;
            return $"[{options.SchemaName}].[{options.WorkItemsTableName}]";
        }
    }
}
