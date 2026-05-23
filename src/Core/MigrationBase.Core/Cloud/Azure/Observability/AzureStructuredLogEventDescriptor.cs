namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Defines a known structured log event name and its operational classification.
/// </summary>
public sealed record AzureStructuredLogEventDescriptor
{
    public required string EventName { get; init; }

    public required string EventCategory { get; init; }

    public AzureStructuredLogSeverity DefaultSeverity { get; init; } = AzureStructuredLogSeverity.Information;

    public bool RequiresCorrelationId { get; init; } = true;

    public bool RequiresMigrationRunId { get; init; }

    public bool RequiresWorkItemId { get; init; }

    public string? Description { get; init; }
}
