namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker;

public sealed class AzureWorkerHeartbeatCheckpointResult
{
    public bool Succeeded { get; init; }

    public string? WorkerId { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? FailureCode { get; init; }

    public string? FailureMessage { get; init; }

    public static AzureWorkerHeartbeatCheckpointResult Success(string workerId, DateTimeOffset recordedAtUtc)
    {
        return new AzureWorkerHeartbeatCheckpointResult
        {
            Succeeded = true,
            WorkerId = workerId,
            RecordedAtUtc = recordedAtUtc
        };
    }

    public static AzureWorkerHeartbeatCheckpointResult Failure(string failureCode, string failureMessage)
    {
        return new AzureWorkerHeartbeatCheckpointResult
        {
            Succeeded = false,
            FailureCode = failureCode,
            FailureMessage = failureMessage
        };
    }
}
