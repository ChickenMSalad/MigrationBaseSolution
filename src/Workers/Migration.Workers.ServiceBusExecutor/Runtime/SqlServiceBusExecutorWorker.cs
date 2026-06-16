using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.Runs;
using Migration.Application.Operational.Telemetry;
using Migration.Application.Operational.WorkItems;
using Migration.Workers.ServiceBusExecutor.Options;
using Migration.Workers.ServiceBusExecutor.Processing;

namespace Migration.Workers.ServiceBusExecutor.Runtime;

internal sealed class SqlServiceBusExecutorWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOperationalWorkItemQueue _workItemQueue;
    private readonly IServiceBusWorkItemExecutor _executor;
    private readonly IOptions<SqlServiceBusExecutorOptions> _options;
    private readonly ILogger<SqlServiceBusExecutorWorker> _logger;
    private readonly IOperationalRunCoordinator _runCoordinator;
    private readonly IConfiguration _configuration;

    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public SqlServiceBusExecutorWorker(
        IOperationalWorkItemQueue workItemQueue,
        IOperationalRunCoordinator runCoordinator,
        IServiceBusWorkItemExecutor executor,
        IOptions<SqlServiceBusExecutorOptions> options,
        ILogger<SqlServiceBusExecutorWorker> logger,
        IConfiguration configuration)
    {
        _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runCoordinator = runCoordinator ?? throw new ArgumentNullException(nameof(runCoordinator));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ServiceBusConnectionString))
        {
            throw new InvalidOperationException("SqlServiceBusExecutor:ServiceBusConnectionString is required.");
        }

        if (string.IsNullOrWhiteSpace(options.QueueName))
        {
            throw new InvalidOperationException("SqlServiceBusExecutor:QueueName is required.");
        }

        _client = new ServiceBusClient(options.ServiceBusConnectionString);
        _processor = _client.CreateProcessor(
            options.QueueName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = Math.Clamp(options.MaxConcurrentCalls, 1, 64)
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _logger.LogInformation(
            "Starting SQL Service Bus executor for queue {QueueName} as worker {WorkerId}.",
            options.QueueName,
            options.WorkerId);

        await _processor.StartProcessingAsync(stoppingToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // normal shutdown
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
            await _processor.DisposeAsync().ConfigureAwait(false);
        }

        if (_client is not null)
        {
            await _client.DisposeAsync().ConfigureAwait(false);
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        ServiceBusWorkItemMessage? message = null;

        try
        {
            message = JsonSerializer.Deserialize<ServiceBusWorkItemMessage>(args.Message.Body, JsonOptions);

            if (message is null)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "INVALID_MESSAGE",
                    "Message did not contain a valid WorkItemId.",
                    args.CancellationToken).ConfigureAwait(false);
                return;
            }

            var workItem = await _workItemQueue.GetAsync(message.WorkItemId, args.CancellationToken).ConfigureAwait(false);

            if (workItem is null)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "WORK_ITEM_NOT_FOUND",
                    $"Work item '{message.WorkItemId}' was not found in SQL.",
                    args.CancellationToken).ConfigureAwait(false);
                return;
            }

            await MarkExecutionStartedAsync(workItem.RunId, workItem.WorkItemId, args.CancellationToken)
                .ConfigureAwait(false);

            using var telemetryScope = _logger.BeginScope(new Dictionary<string, object?>
            {
                [OperationalExecutionTelemetryFields.RunId] = workItem.RunId,
                [OperationalExecutionTelemetryFields.WorkItemId] = workItem.WorkItemId,
                [OperationalExecutionTelemetryFields.ManifestRowId] = workItem.ManifestRowId,
                [OperationalExecutionTelemetryFields.WorkItemType] = workItem.WorkItemType,
                [OperationalExecutionTelemetryFields.AttemptCount] = workItem.AttemptCount,
                [OperationalExecutionTelemetryFields.PartitionKey] = workItem.PartitionKey,
                [OperationalExecutionTelemetryFields.ServiceBusCorrelationId] = args.Message.CorrelationId,
                ["ServiceBusMessageId"] = args.Message.MessageId,
                ["ServiceBusDeliveryCount"] = args.Message.DeliveryCount
            });

            var activityStartedAtUtc = DateTimeOffset.UtcNow;

            using var activity = OperationalExecutionActivity.StartServiceBusWorkItemExecution(
                workItem.RunId,
                workItem.WorkItemId,
                workItem.ManifestRowId,
                workItem.WorkItemType,
                workItem.AttemptCount,
                workItem.PartitionKey,
                args.Message.CorrelationId,
                args.Message.MessageId,
                args.Message.DeliveryCount);

            var result = await _executor.ExecuteAsync(workItem, args.CancellationToken).ConfigureAwait(false);

            if (result.Succeeded)
            {
                await _workItemQueue.CompleteAsync(
                    new CompleteOperationalWorkItemRequest(
                        workItem.WorkItemId,
                        _options.Value.WorkerId,
                        result.ResultJson),
                    args.CancellationToken).ConfigureAwait(false);

                await _runCoordinator.EvaluateCompletionAsync(workItem.RunId, args.CancellationToken)
                    .ConfigureAwait(false);

                OperationalExecutionActivity.SetExecutionDuration(activity, DateTimeOffset.UtcNow - activityStartedAtUtc);
                OperationalExecutionActivity.SetExecutionResult(activity, result.Succeeded, result.ErrorCode);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken).ConfigureAwait(false);
                return;
            }

            var nextAttemptUtc = result.IsRetryable
                ? DateTimeOffset.UtcNow.AddSeconds(Math.Clamp(_options.Value.RetryDelaySeconds, 1, 3600))
                : (DateTimeOffset?)null;

            OperationalExecutionActivity.SetExecutionDuration(activity, DateTimeOffset.UtcNow - activityStartedAtUtc);
            OperationalExecutionActivity.SetExecutionResult(activity, result.Succeeded, result.ErrorCode);

            await _workItemQueue.FailAsync(
                new FailOperationalWorkItemRequest(
                    workItem.WorkItemId,
                    _options.Value.WorkerId,
                    result.ErrorCode ?? "EXECUTION_FAILED",
                    result.ErrorMessage ?? "Service Bus work item execution failed.",
                    result.IsRetryable,
                    nextAttemptUtc),
                args.CancellationToken).ConfigureAwait(false);

            await _runCoordinator.EvaluateCompletionAsync(workItem.RunId, args.CancellationToken)
                .ConfigureAwait(false);

            if (result.IsRetryable)
            {
                await args.CompleteMessageAsync(args.Message, args.CancellationToken).ConfigureAwait(false);
            }
            else
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    result.ErrorCode ?? "EXECUTION_FAILED",
                    result.ErrorMessage,
                    args.CancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while executing Service Bus work item {WorkItemId}.", message?.WorkItemId);
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken).ConfigureAwait(false);
        }
    }

    private async Task MarkExecutionStartedAsync(Guid runId, long workItemId, CancellationToken cancellationToken)
    {
        string connectionString = ResolveOperationalConnectionString();

        const string workItemSql = """
            UPDATE migration.WorkItems
            SET Status = 'Leased',
                LeaseOwner = @WorkerId,
                StartedAtUtc = COALESCE(StartedAtUtc, SYSUTCDATETIME()),
                UpdatedUtc = SYSUTCDATETIME()
            WHERE WorkItemId = @WorkItemId
              AND Status IN ('Dispatching', 'Dispatched', 'Pending', 'Ready', 'Queued', 'FailedRetryable', 'Leased');
            """;

        const string runSql = """
            UPDATE migration.Runs
            SET Status = 'Running',
                StatusReason = COALESCE(StatusReason, 'Executing SQL work items.'),
                StartedAtUtc = COALESCE(StartedAtUtc, SYSUTCDATETIME()),
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE RunId = @RunId
              AND Status NOT IN ('Completed', 'Failed', 'Canceled');
            """;

        const string adminSql = """
            IF OBJECT_ID(N'dbo.AdminRuns', N'U') IS NOT NULL
            BEGIN
                UPDATE dbo.AdminRuns
                SET Status = 'Running',
                    UpdatedUtc = SYSUTCDATETIME()
                WHERE RunId = @RunId
                  AND Status NOT IN ('Completed', 'Failed', 'Canceled');
            END
            """;

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = workItemSql;
            command.Parameters.AddWithValue("@WorkItemId", workItemId);
            command.Parameters.AddWithValue("@WorkerId", _options.Value.WorkerId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = runSql;
            command.Parameters.AddWithValue("@RunId", runId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (SqlCommand command = connection.CreateCommand())
        {
            command.CommandText = adminSql;
            command.Parameters.AddWithValue("@RunId", runId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private string ResolveOperationalConnectionString()
    {
        string? configured = _configuration.GetConnectionString("MigrationOperationalStore");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        configured = _configuration.GetConnectionString("OperationalSql");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        configured = _configuration["SqlOperationalStore:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        configured = _configuration["OperationalStore:Sql:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        throw new InvalidOperationException(
            "SQL operational connection string is required for executor lifecycle state updates.");
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus processor error. Entity: {EntityPath}. Source: {ErrorSource}.",
            args.EntityPath,
            args.ErrorSource);

        return Task.CompletedTask;
    }
}
