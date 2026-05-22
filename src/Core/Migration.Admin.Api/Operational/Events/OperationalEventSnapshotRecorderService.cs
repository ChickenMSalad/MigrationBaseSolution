using System.Text.Json;
using Microsoft.Extensions.Options;
using Migration.Admin.Api.Operational.SqlMetrics;

namespace Migration.Admin.Api.Operational.Events;

public sealed class OperationalEventSnapshotRecorderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OperationalEventSnapshotRecorderOptions> _options;
    private readonly ILogger<OperationalEventSnapshotRecorderService> _logger;

    public OperationalEventSnapshotRecorderService(
        IServiceScopeFactory scopeFactory,
        IOptions<OperationalEventSnapshotRecorderOptions> options,
        ILogger<OperationalEventSnapshotRecorderService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Operational event snapshot recorder is disabled.");
            return;
        }

        var startupDelaySeconds = Math.Clamp(_options.Value.StartupDelaySeconds, 0, 3600);
        if (startupDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(startupDelaySeconds), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RecordSnapshotSafelyAsync(stoppingToken);

            var intervalSeconds = Math.Clamp(_options.Value.IntervalSeconds, 30, 86400);
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task RecordSnapshotSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var metricsReader = scope.ServiceProvider.GetRequiredService<ISqlOperationalMetricsReader>();
            var eventStore = scope.ServiceProvider.GetRequiredService<IOperationalEventStore>();

            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var severity = snapshot.Status == "healthy" ? "info" : "warning";

            if (snapshot.FailureCount > 0 || snapshot.Status == "unhealthy")
            {
                severity = "critical";
            }

            await eventStore.WriteAsync(
                eventType: "ScheduledOperationalMetricsSnapshot",
                severity: severity,
                category: "runtime",
                source: "Migration.Admin.Api",
                message: $"Scheduled operational metrics snapshot recorded with status '{snapshot.Status}'.",
                payloadJson: JsonSerializer.Serialize(snapshot),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Recorded scheduled operational metrics snapshot with status {Status}.",
                snapshot.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to record scheduled operational metrics snapshot.");
        }
    }
}
