namespace MigrationBase.Core.Cloud.Azure.Operations;

/// <summary>
/// A single signal contributing to the operational state of a deployment, host role, or worker lane.
/// </summary>
public sealed class AzureOperationalStateSignal
{
    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public AzureOperationalStateSeverity Severity { get; set; } = AzureOperationalStateSeverity.Informational;

    public bool IsHealthy { get; set; } = true;

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset? ObservedAtUtc { get; set; }
}
