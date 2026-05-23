namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Durable metadata captured when a worker abandons, quarantines, or dead-letters work.
/// </summary>
public sealed class AzureWorkerPoisonWorkRecord
{
    public required string WorkItemId { get; init; }
    public required string RunId { get; init; }
    public string? WorkerId { get; init; }
    public required string ClassificationCode { get; init; }
    public required string Message { get; init; }
    public AzureWorkerPoisonDisposition Disposition { get; init; }
    public int AttemptNumber { get; init; }
    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; init; }
}
