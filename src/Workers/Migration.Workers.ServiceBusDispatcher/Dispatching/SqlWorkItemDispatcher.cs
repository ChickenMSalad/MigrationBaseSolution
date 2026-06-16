using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Workers.ServiceBusDispatcher.Options;
using Migration.Application.Operational.Telemetry;

namespace Migration.Workers.ServiceBusDispatcher.Dispatching;

internal sealed class SqlWorkItemDispatcher : IAsyncDisposable
{
    private readonly SqlServiceBusDispatcherOptions _options;
    private readonly ILogger<SqlWorkItemDispatcher> _logger;
    private readonly ServiceBusClient? _serviceBusClient;
    private readonly ServiceBusSender? _sender;

    public SqlWorkItemDispatcher(
        IOptions<SqlServiceBusDispatcherOptions> options,
        ILogger<SqlWorkItemDispatcher> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.ServiceBusConnectionString) &&
            !string.IsNullOrWhiteSpace(_options.QueueName))
        {
            _serviceBusClient = new ServiceBusClient(_options.ServiceBusConnectionString);
            _sender = _serviceBusClient.CreateSender(_options.QueueName);
        }
    }

    public async Task<int> DispatchNextBatchAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("SQL Service Bus dispatcher is disabled.");
            return 0;
        }

        ValidateOptions();

        await RecoverStaleDispatchingWorkItemsAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<SqlWorkItemDispatchRecord> workItems =
            await ClaimPendingWorkItemsAsync(cancellationToken).ConfigureAwait(false);

        if (workItems.Count == 0)
        {
            return 0;
        }

        foreach (SqlWorkItemDispatchRecord item in workItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await MarkRunRunningAsync(item.RunId, cancellationToken).ConfigureAwait(false);

            ServiceBusWorkItemMessage body = new(
                item.WorkItemId,
                item.RunId,
                item.SequenceNumber,
                item.PayloadJson,
                DateTimeOffset.UtcNow,
                _options.WorkerId);

            string json = JsonSerializer.Serialize(body);

            ServiceBusMessage message = new(json)
            {
                MessageId = item.WorkItemId.ToString("N"),
                CorrelationId = item.RunId.ToString("N"),
                Subject = "migration-work-item"
            };

            message.ApplicationProperties["workItemId"] = item.WorkItemId.ToString("D");
            message.ApplicationProperties["runId"] = item.RunId.ToString("D");
            message.ApplicationProperties["sequenceNumber"] = item.SequenceNumber;
            message.ApplicationProperties["dispatcherId"] = _options.WorkerId;

            var activityStartedAtUtc = DateTimeOffset.UtcNow;

            using var activity = OperationalExecutionActivity.StartServiceBusDispatch(
                item.RunId,
                item.WorkItemId,
                serviceBusCorrelationId: message.CorrelationId,
                serviceBusMessageId: message.MessageId);

            await _sender!.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            await MarkDispatchedAsync(item.WorkItemId, cancellationToken).ConfigureAwait(false);

            OperationalExecutionActivity.SetExecutionDuration(activity, DateTimeOffset.UtcNow - activityStartedAtUtc);
            OperationalExecutionActivity.SetExecutionResult(activity, succeeded: true, errorCode: null);
        }

        _logger.LogInformation(
            "Dispatched {Count} SQL work items to Service Bus queue {QueueName}.",
            workItems.Count,
            _options.QueueName);

        return workItems.Count;
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.SqlConnectionString))
        {
            throw new InvalidOperationException("SqlServiceBusDispatcher:SqlConnectionString is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.ServiceBusConnectionString))
        {
            throw new InvalidOperationException("SqlServiceBusDispatcher:ServiceBusConnectionString is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.QueueName))
        {
            throw new InvalidOperationException("SqlServiceBusDispatcher:QueueName is required.");
        }

        if (_sender is null)
        {
            throw new InvalidOperationException("Service Bus sender was not initialized.");
        }
    }

    private async Task<IReadOnlyList<SqlWorkItemDispatchRecord>> ClaimPendingWorkItemsAsync(CancellationToken cancellationToken)
    {
        const string selectSql = """
            SELECT TOP (@BatchSize)
                   wi.WorkItemId,
                   wi.RunId,
                   CAST(wi.WorkItemId AS bigint) AS SequenceNumber,
                   wi.Status,
                   wi.PayloadJson
            FROM migration.WorkItems wi WITH (READPAST, ROWLOCK)
            WHERE wi.Status IN ('Pending', 'Ready', 'Queued', 'FailedRetryable')
              AND (wi.NotBeforeUtc IS NULL OR wi.NotBeforeUtc <= SYSUTCDATETIME())
            ORDER BY wi.Priority DESC, wi.WorkItemId ASC, wi.CreatedUtc ASC;
            """;

        await using SqlConnection connection = new(_options.SqlConnectionString);

        List<SqlWorkItemDispatchRecord> records = (
            await connection.QueryAsync<SqlWorkItemDispatchRecord>(
                new CommandDefinition(
                    selectSql,
                    new { _options.BatchSize },
                    cancellationToken: cancellationToken))
            ).ToList();

        foreach (SqlWorkItemDispatchRecord record in records)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE migration.WorkItems
                    SET Status = 'Dispatching',
                        LeaseOwner = @WorkerId,
                        LeaseExpiresUtc = NULL,
                        StartedAtUtc = COALESCE(StartedAtUtc, SYSUTCDATETIME()),
                        UpdatedUtc = SYSUTCDATETIME()
                    WHERE WorkItemId = @WorkItemId;
                    """,
                    new { WorkItemId = record.WorkItemId, _options.WorkerId },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        return records;
    }

    private async Task MarkDispatchedAsync(long workItemId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE migration.WorkItems
            SET Status = 'Dispatched',
                StartedAtUtc = COALESCE(StartedAtUtc, SYSUTCDATETIME()),
                DispatchedAtUtc = SYSUTCDATETIME(),
                UpdatedUtc = SYSUTCDATETIME()
            WHERE WorkItemId = @WorkItemId;
            """;

        await using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { WorkItemId = workItemId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    private async Task RecoverStaleDispatchingWorkItemsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE migration.WorkItems
            SET Status = 'Queued',
                LeaseOwner = NULL,
                LeaseExpiresUtc = NULL,
                UpdatedUtc = SYSUTCDATETIME()
            WHERE Status = 'Dispatching'
              AND DispatchedAtUtc IS NULL
              AND UpdatedUtc < DATEADD(second, -30, SYSUTCDATETIME());
            """;

        await using SqlConnection connection = new(_options.SqlConnectionString);
        int recovered = await connection.ExecuteAsync(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (recovered > 0)
        {
            _logger.LogWarning(
                "Recovered {Count} stale SQL work items from Dispatching back to Queued before dispatch polling.",
                recovered);
        }
    }

    private async Task MarkRunRunningAsync(Guid runId, CancellationToken cancellationToken)
    {
        const string operationalSql = """
            UPDATE migration.Runs
            SET Status = 'Running',
                StatusReason = COALESCE(StatusReason, 'Dispatching SQL work items.'),
                StartedAtUtc = COALESCE(StartedAtUtc, SYSUTCDATETIME()),
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE RunId = @RunId
              AND Status NOT IN ('Completed', 'Failed', 'Canceled');
            """;

        const string adminSql = """
            IF OBJECT_ID(N'dbo.AdminRuns', N'U') IS NOT NULL
            BEGIN
                DECLARE @RunKey nvarchar(256);

                SELECT @RunKey = RunKey
                FROM migration.Runs
                WHERE RunId = @RunId;

                UPDATE dbo.AdminRuns
                SET Status = 'Running',
                    UpdatedUtc = SYSUTCDATETIME()
                WHERE (RunId = @RunKey OR RunId = CONVERT(nvarchar(36), @RunId))
                  AND Status NOT IN ('Completed', 'Failed', 'Canceled');
            END
            """;

        await using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(operationalSql, new { RunId = runId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(adminSql, new { RunId = runId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender is not null)
        {
            await _sender.DisposeAsync().ConfigureAwait(false);
        }

        if (_serviceBusClient is not null)
        {
            await _serviceBusClient.DisposeAsync().ConfigureAwait(false);
        }
    }
}
