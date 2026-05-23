namespace MigrationBase.Core.Cloud.Azure.Validation.Throughput;

public sealed class AzureMigrationThroughputValidationResult
{
    public string ProfileName { get; init; } = string.Empty;
    public string RunId { get; init; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; }
    public int ManifestRowsObserved { get; init; }
    public int CompletedItems { get; init; }
    public int FailedItems { get; init; }
    public double AverageItemsPerMinute { get; init; }
    public double PeakItemsPerMinute { get; init; }
    public bool Passed { get; init; }
    public IReadOnlyCollection<string> Findings { get; init; } = Array.Empty<string>();
}
