namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker.Capacity;

public sealed class AzureWorkerCapacityDecision
{
    public AzureWorkerCapacityDecisionKind Kind { get; init; } = AzureWorkerCapacityDecisionKind.Accepted;

    public string ReasonCode { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public TimeSpan? SuggestedDelay { get; init; }

    public Dictionary<string, string> Evidence { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool CanProceed => Kind == AzureWorkerCapacityDecisionKind.Accepted;

    public static AzureWorkerCapacityDecision Accept(string reasonCode = "capacity.available")
    {
        return new AzureWorkerCapacityDecision
        {
            Kind = AzureWorkerCapacityDecisionKind.Accepted,
            ReasonCode = reasonCode,
            Message = "Worker capacity is available."
        };
    }

    public static AzureWorkerCapacityDecision Throttle(string reasonCode, string message, TimeSpan? suggestedDelay = null)
    {
        return new AzureWorkerCapacityDecision
        {
            Kind = AzureWorkerCapacityDecisionKind.Throttled,
            ReasonCode = reasonCode,
            Message = message,
            SuggestedDelay = suggestedDelay
        };
    }
}
