namespace MigrationBase.Core.Cloud.Azure.Observability;

public sealed class AzureHealthSignalSnapshot
{
    public string SignalName { get; init; } = string.Empty;
    public AzureHealthSignalStatus Status { get; init; } = AzureHealthSignalStatus.Unknown;
    public AzureHealthSignalSeverity Severity { get; init; } = AzureHealthSignalSeverity.Unknown;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string> Dimensions { get; init; } = new Dictionary<string, string>();
}
