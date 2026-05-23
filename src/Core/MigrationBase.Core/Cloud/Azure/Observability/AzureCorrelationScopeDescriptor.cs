namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Describes the correlation scope that should travel across a cloud-hosted migration operation.
/// </summary>
public sealed class AzureCorrelationScopeDescriptor
{
    public string CorrelationId { get; init; } = string.Empty;

    public string? ParentCorrelationId { get; init; }

    public string? RunId { get; init; }

    public string? WorkItemId { get; init; }

    public string? WorkerInstanceId { get; init; }

    public string? HostRole { get; init; }

    public string? EnvironmentName { get; init; }

    public string? DeploymentRing { get; init; }

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Dimensions { get; init; } = new Dictionary<string, string>();
}
