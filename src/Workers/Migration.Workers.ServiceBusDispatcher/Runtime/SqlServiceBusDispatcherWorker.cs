using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Workers.ServiceBusDispatcher.Dispatching;
using Migration.Workers.ServiceBusDispatcher.Options;

namespace Migration.Workers.ServiceBusDispatcher.Runtime;

internal sealed class SqlServiceBusDispatcherWorker : BackgroundService
{
    private readonly SqlWorkItemDispatcher _dispatcher;
    private readonly SqlServiceBusDispatcherOptions _options;
    private readonly ILogger<SqlServiceBusDispatcherWorker> _logger;

    public SqlServiceBusDispatcherWorker(
        SqlWorkItemDispatcher dispatcher,
        IOptions<SqlServiceBusDispatcherOptions> options,
        ILogger<SqlServiceBusDispatcherWorker> logger)
    {
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SQL Service Bus dispatcher worker starting. Enabled={Enabled}, Queue={QueueName}.",
            _options.Enabled,
            _options.QueueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DateTimeOffset pollStartedUtc = DateTimeOffset.UtcNow;

                _logger.LogInformation(
                    "P7_EVENT_DISPATCHER_POLL_START WorkerId={WorkerId} Queue={QueueName} BatchSize={BatchSize} PollIntervalSeconds={PollIntervalSeconds} Utc={Utc}.",
                    _options.WorkerId,
                    _options.QueueName,
                    _options.BatchSize,
                    _options.PollIntervalSeconds,
                    pollStartedUtc);

                await _dispatcher.DispatchNextBatchAsync(stoppingToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "P7_EVENT_DISPATCHER_POLL_COMPLETE WorkerId={WorkerId} Queue={QueueName} DurationMs={DurationMs} Utc={Utc}.",
                    _options.WorkerId,
                    _options.QueueName,
                    (DateTimeOffset.UtcNow - pollStartedUtc).TotalMilliseconds,
                    DateTimeOffset.UtcNow);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "P7_EVENT_DISPATCHER_POLL_FAILED WorkerId={WorkerId} Queue={QueueName}. SQL Service Bus dispatcher iteration failed.",
                    _options.WorkerId,
                    _options.QueueName);
            }

            int delaySeconds = Math.Max(1, _options.PollIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("SQL Service Bus dispatcher worker stopped.");
    }
}
