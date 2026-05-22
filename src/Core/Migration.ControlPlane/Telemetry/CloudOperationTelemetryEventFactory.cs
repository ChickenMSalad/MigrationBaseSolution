namespace Migration.ControlPlane.Telemetry;

public static class CloudOperationTelemetryEventFactory
{
    public static TelemetryEventWriteRequest ReadinessChecked(
        string workspaceId,
        bool isCloudReady,
        int warningCount)
    {
        return new TelemetryEventWriteRequest(
            WorkspaceId: workspaceId,
            EventName: CloudOperationTelemetryEventNames.ReadinessChecked,
            Category: TelemetryCategories.Cloud,
            Severity: warningCount > 0 ? "warning" : "information",
            Dimensions: new Dictionary<string, string>
            {
                ["isCloudReady"] = isCloudReady.ToString()
            },
            Metrics: new Dictionary<string, double>
            {
                ["cloud.readiness.checked"] = 1,
                ["cloud.readiness.warning_count"] = warningCount,
                ["cloud.readiness.ready"] = isCloudReady ? 1 : 0
            });
    }

    public static TelemetryEventWriteRequest ProviderChecked(
        string workspaceId,
        string eventName,
        string providerKind,
        bool isConfigured,
        int warningCount)
    {
        return new TelemetryEventWriteRequest(
            WorkspaceId: workspaceId,
            EventName: eventName,
            Category: TelemetryCategories.Cloud,
            Severity: isConfigured && warningCount == 0 ? "information" : "warning",
            Dimensions: new Dictionary<string, string>
            {
                ["providerKind"] = providerKind,
                ["isConfigured"] = isConfigured.ToString()
            },
            Metrics: new Dictionary<string, double>
            {
                ["cloud.provider.checked"] = 1,
                ["cloud.provider.configured"] = isConfigured ? 1 : 0,
                ["cloud.provider.warning_count"] = warningCount
            });
    }
}
