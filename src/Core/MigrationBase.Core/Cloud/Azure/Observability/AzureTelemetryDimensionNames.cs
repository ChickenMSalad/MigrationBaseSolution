namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Canonical telemetry dimension names for migration cloud runtime events.
/// </summary>
public static class AzureTelemetryDimensionNames
{
    public const string CorrelationId = "correlation.id";
    public const string ParentCorrelationId = "correlation.parent_id";
    public const string RunId = "migration.run_id";
    public const string WorkItemId = "migration.work_item_id";
    public const string WorkerInstanceId = "worker.instance_id";
    public const string HostRole = "host.role";
    public const string EnvironmentName = "environment.name";
    public const string DeploymentRing = "deployment.ring";
    public const string TenantBoundary = "tenant.boundary";
    public const string MigrationSource = "migration.source";
    public const string MigrationTarget = "migration.target";
}
