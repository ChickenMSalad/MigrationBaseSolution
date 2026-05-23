namespace MigrationBase.Core.Cloud.Azure.Observability.Anomalies;

public sealed class AzureAnomalyDetectionWindow
{
    public int DurationSeconds { get; init; } = 300;

    public int MinimumSamples { get; init; } = 3;

    public int ConsecutiveBreachesRequired { get; init; } = 1;
}
