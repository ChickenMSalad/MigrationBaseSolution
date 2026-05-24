using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Infrastructure.Runtime.SqlServer;

namespace Migration.Infrastructure.Runtime.Hosted;

public sealed class SqlOperationalWorkerHostedService : BackgroundService
{
    private readonly SqlOperationalQueueRuntime _runtime;
    private readonly IOptionsMonitor<SqlOperationalWorkerOptions> _options;
    private readonly ILogger<SqlOperationalWorkerHostedService> _logger;

    public SqlOperationalWorkerHostedService(
        SqlOperationalQueueRuntime runtime,
        IOptionsMonitor<SqlOperationalWorkerOptions> options,
        ILogger<SqlOperationalWorkerHostedService> logger)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SqlOperationalWorkerOptions configured = _options.CurrentValue;

        if (!configured.Enabled)
        {
            _logger.LogInformation("SQL operational worker is disabled. Set {OptionsSection}:Enabled=true to run it.", SqlOperationalWorkerOptions.SectionName);
            return;
        }

        SqlOperationalQueueRuntimeOptions runtimeOptions = ToRuntimeOptions(configured);

        _logger.LogInformation(
            "Starting SQL operational worker {WorkerId}. BatchSize={BatchSize}, LeaseSeconds={LeaseSeconds}, RunId={RunId}, RunUntilIdleAndStop={RunUntilIdleAndStop}",
            runtimeOptions.WorkerId,
            runtimeOptions.BatchSize,
            runtimeOptions.LeaseSeconds,
            runtimeOptions.RunId,
            configured.RunUntilIdleAndStop);

        try
        {
            SqlOperationalQueueRuntimeResult result = configured.RunUntilIdleAndStop
                ? await _runtime.RunUntilIdleAsync(runtimeOptions with { MaxConsecutiveIdlePolls = 1 }, stoppingToken).ConfigureAwait(false)
                : await _runtime.RunContinuousAsync(runtimeOptions, stoppingToken).ConfigureAwait(false);

            _logger.LogInformation(
                "SQL operational worker stopped. Claimed={ClaimedCount}, Completed={CompletedCount}, Failed={FailedCount}, RetryScheduled={RetryScheduledCount}, IdlePolls={IdlePollCount}",
                result.ClaimedCount,
                result.CompletedCount,
                result.FailedCount,
                result.RetryScheduledCount,
                result.IdlePollCount);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("SQL operational worker cancellation requested.");
        }
    }

    private static SqlOperationalQueueRuntimeOptions ToRuntimeOptions(SqlOperationalWorkerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new SqlOperationalQueueRuntimeOptions(
            string.IsNullOrWhiteSpace(options.WorkerId) ? Environment.MachineName : options.WorkerId,
            options.BatchSize <= 0 ? 25 : options.BatchSize,
            options.LeaseSeconds <= 0 ? 300 : options.LeaseSeconds,
            options.RetryDelaySeconds <= 0 ? 300 : options.RetryDelaySeconds,
            options.IdleDelayMilliseconds < 0 ? 0 : options.IdleDelayMilliseconds,
            0,
            options.RunId);
    }
}
