using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class ExecutionReplayAdmissionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<ExecutionReplayAdmissionBackgroundOptions> _options;
    private readonly ILogger<ExecutionReplayAdmissionBackgroundService> _logger;

    public ExecutionReplayAdmissionBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<ExecutionReplayAdmissionBackgroundOptions> options,
        ILogger<ExecutionReplayAdmissionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            var intervalSeconds = Math.Clamp(options.IntervalSeconds, 15, 3600);

            if (options.Enabled)
            {
                await EvaluateAdmissionAsync(options, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task EvaluateAdmissionAsync(
        ExecutionReplayAdmissionBackgroundOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IExecutionReplayAdmissionService>();

            var result = await service.EvaluateAsync(
                new EvaluateExecutionReplayAdmissionRequest(Math.Clamp(options.Take, 1, 250)),
                cancellationToken);

            if (result.Decisions.Count > 0)
            {
                _logger.LogInformation(
                    "Replay admission background evaluator processed {DecisionCount} decision(s).",
                    result.Decisions.Count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Replay admission background evaluator failed.");
        }
    }
}
