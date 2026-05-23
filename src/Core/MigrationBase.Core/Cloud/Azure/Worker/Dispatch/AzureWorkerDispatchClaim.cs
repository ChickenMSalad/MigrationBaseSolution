namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchClaim
{
    public required string ClaimId { get; init; }

    public required string DispatchId { get; init; }

    public required string WorkItemId { get; init; }

    public required string WorkerId { get; init; }

    public DateTimeOffset ClaimedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAtUtc { get; init; }

    public bool IsExpired(DateTimeOffset utcNow)
    {
        return ExpiresAtUtc <= utcNow;
    }
}
