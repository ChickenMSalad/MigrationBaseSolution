using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Workers.ServiceBusDispatcher.Options;

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

        IReadOnlyList<SqlWorkItemDispatchRecord> workItems = await ClaimPendingWorkItemsAsync(cancellationToken).ConfigureAwait(false);

        if (workItems.Count == 0)
        {
            return 0;
        }

        foreach (SqlWorkItemDispatchRecord item in workItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

            await _sender!.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            await MarkDispatchedAsync(item.WorkItemId, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Dispatched {Count} SQL work items to Service Bus queue {QueueName}.", workItems.Count, _options.QueueName);
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
        const string sql = """
;WITH next_items AS
(
    SELECT TOP (@BatchSize)
        wi.WorkItemId,
        wi.RunId,
        wi.SequenceNumber,
        wi.Status,
        wi.PayloadJson
    FROM dbo.MigrationWorkItems wi WITH (READPAST, UPDLOCK, ROWLOCK)
    WHERE wi.Status IN ('Pending', 'Ready')
      AND (wi.LeaseExpiresAtUtc IS NULL OR wi.LeaseExpiresAtUtc < SYSUTCDATETIME())
    ORDER BY wi.Priority DESC, wi.SequenceNumber ASC, wi.CreatedAtUtc ASC
)
UPDATE next_items
SET
    Status = 'Dispatching',
    LeaseOwner = @WorkerId,
    LeaseExpiresAtUtc = DATEADD(second, @LeaseSeconds, SYSUTCDATETIME()),
    UpdatedAtUtc = SYSUTCDATETIME()
OUTPUT
    inserted.WorkItemId,
    inserted.RunId,
    inserted.SequenceNumber,
    inserted.Status,
    inserted.PayloadJson;
""";

        await using SqlConnection connection = new(_options.SqlConnectionString);
        IEnumerable<SqlWorkItemDispatchRecord> records = await connection.QueryAsync<SqlWorkItemDispatchRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    _options.BatchSize,
                    _options.WorkerId,
                    _options.LeaseSeconds
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return records.ToList();
    }

    private async Task MarkDispatchedAsync(Guid workItemId, CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE dbo.MigrationWorkItems
SET
    Status = 'Dispatched',
    DispatchedAtUtc = SYSUTCDATETIME(),
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE WorkItemId = @WorkItemId;
""";

        await using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { WorkItemId = workItemId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
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
