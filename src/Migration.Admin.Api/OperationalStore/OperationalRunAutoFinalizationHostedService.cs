using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunAutoFinalizationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OperationalRunAutoFinalizationOptions> _options;
    private readonly ILogger<OperationalRunAutoFinalizationHostedService> _logger;

    public OperationalRunAutoFinalizationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<OperationalRunAutoFinalizationOptions> options,
        ILogger<OperationalRunAutoFinalizationHostedService> logger)
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
            _logger.LogInformation(
                "Operational run auto-finalization hosted service is disabled.");

            return;
        }

        var interval = TimeSpan.FromSeconds(
            Math.Clamp(_options.Value.IntervalSeconds, 5, 3600));

        _logger.LogInformation(
            "Operational run auto-finalization hosted service started with interval {Interval}.",
            interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var service = scope.ServiceProvider
                    .GetRequiredService<IOperationalRunAutoFinalizationService>();

                await service.FinalizeEligibleRunsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Operational run auto-finalization cycle failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
