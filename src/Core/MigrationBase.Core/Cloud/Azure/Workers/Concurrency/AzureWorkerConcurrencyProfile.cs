namespace MigrationBase.Core.Cloud.Azure.Workers.Concurrency;

public sealed class AzureWorkerConcurrencyProfile
{
    public string Name { get; init; } = string.Empty;
    public string WorkerRole { get; init; } = string.Empty;
    public int MaxConcurrentRuns { get; init; } = 1;
    public int MaxConcurrentWorkItems { get; init; } = 1;
    public int MaxConcurrentWorkItemsPerRun { get; init; } = 1;
    public int PrefetchCount { get; init; } = 0;
    public int DrainTargetSeconds { get; init; } = 30;
    public bool AllowDynamicScaleDown { get; init; } = true;
    public bool AllowDynamicScaleUp { get; init; } = false;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
