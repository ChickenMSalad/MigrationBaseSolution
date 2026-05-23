namespace MigrationBase.Core.Cloud.Azure.Execution;

public sealed class AzureExecutionEnvironmentProfile
{
    public string Name { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string DeploymentRing { get; init; } = string.Empty;
    public string HostRole { get; init; } = string.Empty;
    public bool AllowsRealMigrationExecution { get; init; }
    public bool RequiresReadinessEvidence { get; init; } = true;
    public bool RequiresOperatorApproval { get; init; } = true;
    public int MaxConcurrentRuns { get; init; } = 1;
    public int MaxConcurrentWorkItems { get; init; } = 1;
    public string OperationalStoreName { get; init; } = "sql";
    public string QueueProfileName { get; init; } = string.Empty;
    public string TelemetryProfileName { get; init; } = string.Empty;
}
