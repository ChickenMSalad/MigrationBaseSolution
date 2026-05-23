namespace MigrationBase.Core.Cloud.Azure.Workers.Lifecycle;

/// <summary>
/// Defines conservative defaults for graceful Azure worker shutdown.
/// </summary>
public sealed class AzureWorkerShutdownPolicy
{
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan DrainGracePeriod { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan HeartbeatFinalizationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool StopPollingBeforeDrain { get; set; } = true;

    public bool MarkWorkerUnavailableDuringDrain { get; set; } = true;

    public bool AllowAbandonAfterTimeout { get; set; }
}
