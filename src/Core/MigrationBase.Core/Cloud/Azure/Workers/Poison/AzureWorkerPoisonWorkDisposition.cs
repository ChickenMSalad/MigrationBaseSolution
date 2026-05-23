namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Durable disposition result for a poison work item after policy evaluation or operator review.
/// </summary>
public sealed class AzureWorkerPoisonWorkDisposition
{
    public string WorkItemId { get; init; } = string.Empty;
    public string RunId { get; init; } = string.Empty;
    public string ClassificationCode { get; init; } = string.Empty;
    public AzureWorkerPoisonWorkAction Action { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? OperatorId { get; init; }
    public DateTimeOffset DispositionedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
