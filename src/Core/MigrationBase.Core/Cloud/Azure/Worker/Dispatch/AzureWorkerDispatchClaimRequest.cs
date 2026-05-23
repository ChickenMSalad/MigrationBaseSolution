namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchClaimRequest
{
    public required AzureWorkerDispatchEnvelope Envelope { get; init; }

    public required string WorkerId { get; init; }

    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(5);

    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
