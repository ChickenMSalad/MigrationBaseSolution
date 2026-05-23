namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchEnvelope
{
    public required string DispatchId { get; init; }

    public required string WorkItemId { get; init; }

    public string? RunId { get; init; }

    public string? ManifestId { get; init; }

    public string? SourceSystem { get; init; }

    public string? TargetSystem { get; init; }

    public int AttemptNumber { get; init; }

    public DateTimeOffset EnqueuedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? NotBeforeUtc { get; init; }

    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
