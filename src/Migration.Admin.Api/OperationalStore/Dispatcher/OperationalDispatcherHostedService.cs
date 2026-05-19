using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OperationalDispatcherOptions> _options;
    private readonly ILogger<OperationalDispatcherHostedService> _logger;

    public OperationalDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<OperationalDispatcherOptions> options,
        ILogger<OperationalDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Operational dispatcher hosted service is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(
            Math.Clamp(_options.Value.PollingIntervalSeconds, 5, 3600));

        _logger.LogInformation(
            "Operational dispatcher hosted service started with interval {Interval}.",
            interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var dispatcher = scope.ServiceProvider
                    .GetRequiredService<IOperationalDispatcherService>();

                await dispatcher.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operational dispatcher cycle failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
