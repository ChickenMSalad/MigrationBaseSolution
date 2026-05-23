namespace MigrationBase.Core.Cloud.Azure.Observability.Anomalies;

public sealed class AzureAnomalyDetectionThreshold
{
    public string Operator { get; init; } = string.Empty;

    public decimal Value { get; init; }

    public string Unit { get; init; } = string.Empty;

    public string BaselineMode { get; init; } = string.Empty;
}
