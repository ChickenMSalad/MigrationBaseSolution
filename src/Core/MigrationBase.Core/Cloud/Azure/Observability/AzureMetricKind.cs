namespace MigrationBase.Core.Cloud.Azure.Observability;

public enum AzureMetricKind
{
    Counter = 0,
    Gauge = 1,
    Histogram = 2,
    Duration = 3,
    Rate = 4
}
