namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Describes a normalized operational log event emitted by cloud-hosted migration components.
/// </summary>
public sealed record AzureStructuredLogEvent
{
    public required string EventName { get; init; }

    public required string EventCategory { get; init; }

    public AzureStructuredLogSeverity Severity { get; init; } = AzureStructuredLogSeverity.Information;

    public string? Message { get; init; }

    public string? EnvironmentName { get; init; }

    public string? DeploymentRing { get; init; }

    public string? HostRole { get; init; }

    public string? WorkerInstanceId { get; init; }

    public string? MigrationRunId { get; init; }

    public string? WorkItemId { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset ObservedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}
