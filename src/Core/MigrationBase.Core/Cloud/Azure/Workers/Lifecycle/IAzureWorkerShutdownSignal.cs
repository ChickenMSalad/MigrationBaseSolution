namespace MigrationBase.Core.Cloud.Azure.Workers.Lifecycle;

/// <summary>
/// Provides a host-neutral shutdown signal that later worker hosts can bridge to IHostApplicationLifetime.
/// </summary>
public interface IAzureWorkerShutdownSignal
{
    bool IsShutdownRequested { get; }

    DateTimeOffset? RequestedAtUtc { get; }

    string Reason { get; }

    CancellationToken ShutdownToken { get; }
}
