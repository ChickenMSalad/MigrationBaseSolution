namespace MigrationBase.Core.Cloud.Azure.Workers.Lifecycle;

/// <summary>
/// Represents an operator/runtime request to drain a worker before shutdown, deployment, scale-in, or maintenance.
/// </summary>
public sealed class AzureWorkerDrainRequest
{
    public string WorkerId { get; set; } = string.Empty;

    public string HostRole { get; set; } = string.Empty;

    public AzureWorkerDrainMode Mode { get; set; } = AzureWorkerDrainMode.StopAcceptingNewWork;

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromMinutes(5);

    public bool AllowWorkAbandonmentAfterGracePeriod { get; set; }
}
