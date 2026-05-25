namespace Migration.Application.Operational.Telemetry;

public static class OperationalTelemetryConfigurationKeys
{
    public const string OpenTelemetrySectionName = OperationalTelemetryRegistrationOptions.SectionName;
    public const string ApplicationInsightsConnectionString = "ApplicationInsights:ConnectionString";
    public const string ApplicationInsightsEnvironmentVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";
    public const string AzureMonitorConnectionString = "OpenTelemetry:AzureMonitorConnectionString";
}
