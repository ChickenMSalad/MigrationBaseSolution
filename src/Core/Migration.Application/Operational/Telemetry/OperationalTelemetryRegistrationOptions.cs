namespace Migration.Application.Operational.Telemetry;

public sealed class OperationalTelemetryRegistrationOptions
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; set; }

    public string ServiceName { get; set; } = "MigrationBaseSolution";

    public string ServiceNamespace { get; set; } = "Migration";

    public string ServiceInstanceId { get; set; } = string.Empty;

    public bool EnableTracing { get; set; } = true;

    public bool EnableMetrics { get; set; }

    public bool EnableConsoleExporter { get; set; }

    public bool EnableAzureMonitorExporter { get; set; }

    public string AzureMonitorConnectionString { get; set; } = string.Empty;

    public double TraceSamplingRatio { get; set; } = 1.0d;
}
