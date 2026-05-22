using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.Leases;

namespace Migration.Infrastructure.Sql.Operational.Leases;

public sealed class SqlOperationalWorkItemLeaseCoordinator : IOperationalWorkItemLeaseCoordinator
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<SqlOperationalWorkItemLeaseCoordinatorOptions> _options;

    public SqlOperationalWorkItemLeaseCoordinator(
        IConfiguration configuration,
        IOptions<SqlOperationalWorkItemLeaseCoordinatorOptions> options)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<OperationalWorkItemLeaseRenewalResult> RenewLeaseAsync(
        RenewOperationalWorkItemLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTimeOffset.UtcNow;
        var leaseSeconds = ClampLeaseSeconds(request.LeaseSeconds);
        var leaseExpiresUtc = now.AddSeconds(leaseSeconds);

        var sql = $@"
update {TableName}
set LeaseExpiresUtc = @LeaseExpiresUtc,
    UpdatedUtc = @UpdatedUtc
output inserted.WorkItemId,
       inserted.LeaseOwner as WorkerId,
       inserted.LeaseExpiresUtc,
       inserted.Status
where WorkItemId = @WorkItemId
  and LeaseOwner = @WorkerId
  and Status = 'Leased'
  and LeaseExpiresUtc > @NowUtc;";

        await using var connection = OpenConnection();
        var row = await connection.QuerySingleOrDefaultAsync<LeaseRenewalRow>(new CommandDefinition(sql, new
        {
            request.WorkItemId,
            request.WorkerId,
            LeaseExpiresUtc = leaseExpiresUtc,
            UpdatedUtc = now,
            NowUtc = now
        }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return new OperationalWorkItemLeaseRenewalResult(
                request.WorkItemId,
                request.WorkerId,
                false,
                null,
                null,
                "Lease was not renewed. The item may not be leased by this worker, may not exist, or the lease may already be expired.");
        }

        return new OperationalWorkItemLeaseRenewalResult(
            row.WorkItemId,
            row.WorkerId ?? request.WorkerId,
            true,
            row.LeaseExpiresUtc,
            row.Status,
            null);
    }

    public async Task<OperationalWorkItemLeaseReleaseResult> ReleaseExpiredLeasesAsync(
        ReleaseExpiredOperationalWorkItemLeasesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTimeOffset.UtcNow;
        var maxItems = Math.Clamp(
            request.MaxItemsToRelease,
            1,
            Math.Max(1, _options.Value.MaxExpiredLeaseReleaseBatchSize));

        var sql = $@"
;with ExpiredLeases as (
    select top (@MaxItems) WorkItemId
    from {TableName} with (rowlock, readpast, updlock)
    where Status = 'Leased'
      and LeaseExpiresUtc is not null
      and LeaseExpiresUtc <= @NowUtc
      and (@RunId is null or RunId = @RunId)
      and (@WorkerId is null or LeaseOwner = @WorkerId)
    order by LeaseExpiresUtc asc, CreatedUtc asc
)
update wi
set Status = case when wi.AttemptCount < wi.MaxAttempts then 'FailedRetryable' else 'Failed' end,
    LeaseOwner = null,
    LeaseExpiresUtc = null,
    NotBeforeUtc = case when wi.AttemptCount < wi.MaxAttempts then @NowUtc else wi.NotBeforeUtc end,
    LastErrorCode = coalesce(wi.LastErrorCode, 'LEASE_EXPIRED'),
    LastErrorMessage = coalesce(wi.LastErrorMessage, 'Work item lease expired before completion.'),
    UpdatedUtc = @NowUtc
from {TableName} wi
inner join ExpiredLeases expired on expired.WorkItemId = wi.WorkItemId;";

        await using var connection = OpenConnection();
        var released = await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            request.RunId,
            request.WorkerId,
            MaxItems = maxItems,
            NowUtc = now
        }, cancellationToken: cancellationToken));

        return new OperationalWorkItemLeaseReleaseResult(released, now);
    }

    public async Task<OperationalWorkItemLeaseSnapshot> GetLeaseSnapshotAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var sql = $@"
select
    @RunId as RunId,
    sum(case when Status = 'Leased' and LeaseExpiresUtc > @NowUtc then 1 else 0 end) as ActiveLeaseCount,
    sum(case when Status = 'Leased' and LeaseExpiresUtc <= @NowUtc then 1 else 0 end) as ExpiredLeaseCount,
    sum(case when Status = 'Pending' then 1 else 0 end) as PendingCount,
    sum(case when Status = 'FailedRetryable' then 1 else 0 end) as FailedRetryableCount,
    min(case when Status = 'Leased' and LeaseExpiresUtc > @NowUtc then LeaseExpiresUtc else null end) as OldestActiveLeaseUtc,
    min(case when Status = 'Leased' and LeaseExpiresUtc <= @NowUtc then LeaseExpiresUtc else null end) as OldestExpiredLeaseUtc,
    @NowUtc as SnapshotUtc
from {TableName}
where RunId = @RunId;";

        await using var connection = OpenConnection();
        return await connection.QuerySingleAsync<OperationalWorkItemLeaseSnapshot>(new CommandDefinition(sql, new
        {
            RunId = runId,
            NowUtc = now
        }, cancellationToken: cancellationToken));
    }

    private int ClampLeaseSeconds(int requestedLeaseSeconds)
    {
        var options = _options.Value;
        var requested = requestedLeaseSeconds <= 0 ? options.DefaultLeaseSeconds : requestedLeaseSeconds;
        return Math.Clamp(requested, 30, Math.Max(30, options.MaxLeaseSeconds));
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
            "SQL operational lease coordinator connection string is not configured. Configure ConnectionStrings:MigrationOperationalStore or SqlOperationalWorkItemLeaseCoordinator:ConnectionString.");
    }

    private string TableName
    {
        get
        {
            var options = _options.Value;
            return $"[{options.SchemaName}].[{options.WorkItemsTableName}]";
        }
    }

    private sealed record LeaseRenewalRow(
        Guid WorkItemId,
        string? WorkerId,
        DateTimeOffset? LeaseExpiresUtc,
        string? Status);
}
