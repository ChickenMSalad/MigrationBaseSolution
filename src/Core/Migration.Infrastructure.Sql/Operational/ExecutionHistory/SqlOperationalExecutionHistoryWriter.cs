using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.ExecutionHistory;

namespace Migration.Infrastructure.Sql.Operational.ExecutionHistory;

public sealed class SqlOperationalExecutionHistoryWriter : IOperationalExecutionHistoryWriter
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<SqlOperationalExecutionHistoryOptions> _options;

    public SqlOperationalExecutionHistoryWriter(
        IConfiguration configuration,
        IOptions<SqlOperationalExecutionHistoryOptions> options)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<long> RecordStartedAsync(
        OperationalExecutionAttemptStarted request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sql = $@"
insert into {ExecutionAttemptsTableName}
(
    RunId,
    WorkItemId,
    ManifestRowId,
    WorkItemType,
    WorkerId,
    AttemptNumber,
    PartitionKey,
    PayloadJson,
    Status,
    StartedAtUtc,
    CreatedAtUtc,
    UpdatedAtUtc
)
output inserted.ExecutionAttemptId
values
(
    @RunId,
    @WorkItemId,
    @ManifestRowId,
    @WorkItemType,
    @WorkerId,
    @AttemptNumber,
    @PartitionKey,
    @PayloadJson,
    N'Started',
    @StartedAtUtc,
    @StartedAtUtc,
    @StartedAtUtc
);";

        await using var connection = OpenConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            request,
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task RecordCompletedAsync(
        OperationalExecutionAttemptCompleted request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sql = $@"
update {ExecutionAttemptsTableName}
set Status = N'Completed',
    ResultJson = @ResultJson,
    CompletedAtUtc = @CompletedAtUtc,
    UpdatedAtUtc = @CompletedAtUtc
where ExecutionAttemptId = @ExecutionAttemptId
  and RunId = @RunId
  and WorkItemId = @WorkItemId;";

        await using var connection = OpenConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            request,
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task RecordFailedAsync(
        OperationalExecutionAttemptFailed request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sql = $@"
update {ExecutionAttemptsTableName}
set Status = case when @IsRetryable = 1 then N'FailedRetryable' else N'FailedTerminal' end,
    ErrorCode = @ErrorCode,
    ErrorMessage = @ErrorMessage,
    IsRetryable = @IsRetryable,
    FailureJson = @FailureJson,
    FailedAtUtc = @FailedAtUtc,
    NextAttemptUtc = @NextAttemptUtc,
    UpdatedAtUtc = @FailedAtUtc
where ExecutionAttemptId = @ExecutionAttemptId
  and RunId = @RunId
  and WorkItemId = @WorkItemId;";

        await using var connection = OpenConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            request,
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(ResolveConnectionString());
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

        var fallback = _configuration["SqlOperationalExecutionHistory:ConnectionString"]
            ?? _configuration["SqlOperationalStore:ConnectionString"]
            ?? _configuration["OperationalStore:Sql:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        throw new InvalidOperationException(
            "SQL operational execution history connection string is not configured. Configure ConnectionStrings:MigrationOperationalStore or SqlOperationalExecutionHistory:ConnectionString.");
    }

    private string ExecutionAttemptsTableName => $"[{_options.Value.SchemaName}].[{_options.Value.ExecutionAttemptsTableName}]";
}
