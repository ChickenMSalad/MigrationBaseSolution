namespace MigrationBase.Core.Cloud.Azure.ExecutionIsolation;

public sealed record AzureExecutionIsolationBoundary(
    string Name,
    string EnvironmentName,
    string DeploymentRing,
    string TenantBoundary,
    string WorkloadBoundary,
    bool AllowsSharedWorkers,
    bool AllowsSharedStorage,
    bool AllowsSharedQueueNamespace,
    bool RequiresDedicatedSqlDatabase);
