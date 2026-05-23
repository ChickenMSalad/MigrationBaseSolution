namespace MigrationBase.Core.Cloud.Azure.Observability.Alerts;

public sealed class AzureAlertThreshold
{
    public string Operator { get; init; } = "GreaterThan";
    public decimal Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string Aggregation { get; init; } = "Average";
}
