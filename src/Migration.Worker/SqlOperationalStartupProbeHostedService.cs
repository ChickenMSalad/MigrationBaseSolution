using Migration.Infrastructure.Runtime.Composition;

namespace Migration.Worker;

internal sealed class SqlOperationalStartupProbeHostedService : IHostedService
{
    private readonly SqlOperationalRuntimeReadinessProbe _readinessProbe;
    private readonly ILogger<SqlOperationalStartupProbeHostedService> _logger;

    public SqlOperationalStartupProbeHostedService(
        SqlOperationalRuntimeReadinessProbe readinessProbe,
        ILogger<SqlOperationalStartupProbeHostedService> logger)
    {
        _readinessProbe = readinessProbe ?? throw new ArgumentNullException(nameof(readinessProbe));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        SqlOperationalRuntimeReadinessResult result = await _readinessProbe.CheckAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsReady)
        {
            _logger.LogInformation("SQL operational runtime readiness passed: {Message}", result.Message);
            return;
        }

        _logger.LogError(result.Exception, "SQL operational runtime readiness failed: {Message}", result.Message);
        throw new InvalidOperationException(result.Message, result.Exception);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
