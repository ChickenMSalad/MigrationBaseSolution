namespace MigrationBase.Core.Cloud.Azure;

public sealed class AzureTelemetryOptions
{
    public string ApplicationInsightsConnectionStringName { get; set; } = "ApplicationInsights";

    public bool EnableApplicationInsights { get; set; } = true;

    public bool EnableStructuredOperationalEvents { get; set; } = true;

    public bool EnableWorkerHeartbeatMetrics { get; set; } = true;

    public bool EnableRunCorrelation { get; set; } = true;

    public int MetricsFlushIntervalSeconds { get; set; } = 30;
}
