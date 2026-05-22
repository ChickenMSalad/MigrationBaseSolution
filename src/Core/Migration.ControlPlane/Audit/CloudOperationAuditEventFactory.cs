namespace Migration.ControlPlane.Audit;

public static class CloudOperationAuditEventFactory
{
    public static AuditEventWriteRequest Create(
        string workspaceId,
        string eventName,
        string actor,
        IReadOnlyDictionary<string, string>? properties = null,
        string severity = "information",
        string? correlationId = null)
    {
        return new AuditEventWriteRequest(
            WorkspaceId: string.IsNullOrWhiteSpace(workspaceId) ? "default" : workspaceId,
            Category: AuditCategories.Cloud,
            EventName: string.IsNullOrWhiteSpace(eventName) ? "cloud.operation.checked" : eventName,
            Severity: string.IsNullOrWhiteSpace(severity) ? "information" : severity,
            CorrelationId: correlationId,
            Actor: string.IsNullOrWhiteSpace(actor) ? "admin-api" : actor,
            Properties: properties ?? new Dictionary<string, string>());
    }

    public static AuditEventWriteRequest ReadinessChecked(
        string workspaceId,
        bool isCloudReady,
        int warningCount)
    {
        return Create(
            workspaceId,
            CloudOperationAuditEventNames.ReadinessChecked,
            "admin-api",
            new Dictionary<string, string>
            {
                ["isCloudReady"] = isCloudReady.ToString(),
                ["warningCount"] = warningCount.ToString()
            },
            warningCount > 0 ? "warning" : "information");
    }

    public static AuditEventWriteRequest ProviderChecked(
        string workspaceId,
        string eventName,
        string providerKind,
        bool isConfigured,
        int warningCount)
    {
        return Create(
            workspaceId,
            eventName,
            "admin-api",
            new Dictionary<string, string>
            {
                ["providerKind"] = providerKind,
                ["isConfigured"] = isConfigured.ToString(),
                ["warningCount"] = warningCount.ToString()
            },
            isConfigured && warningCount == 0 ? "information" : "warning");
    }
}
