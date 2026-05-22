using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.Operational.Events;

public sealed class OperationalEventRetentionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OperationalEventRetentionOptions> _options;
    private readonly ILogger<OperationalEventRetentionWorker> _logger;

    public OperationalEventRetentionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OperationalEventRetentionOptions> options,
        ILogger<OperationalEventRetentionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Operational event retention worker is disabled.");
            return;
        }

        var startupDelaySeconds = Math.Clamp(_options.Value.StartupDelaySeconds, 0, 3600);
        if (startupDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(startupDelaySeconds), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await PruneSafelyAsync(stoppingToken);

            var intervalHours = Math.Clamp(_options.Value.IntervalHours, 1, 168);
            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }

    private async Task PruneSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var retentionService = scope.ServiceProvider.GetRequiredService<IOperationalEventRetentionService>();

            var retentionDays = Math.Clamp(_options.Value.RetentionDays, 1, 3650);
            var result = await retentionService.PruneAsync(retentionDays, cancellationToken);

            _logger.LogInformation(
                "Operational event retention deleted {DeletedEvents} event(s) older than {CutoffUtc}.",
                result.DeletedEvents,
                result.CutoffUtc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Operational event retention failed.");
        }
    }
}
