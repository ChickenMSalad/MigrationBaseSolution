namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Provides the baseline P5 Azure deployment readiness checks used by deployment automation and operator review.
/// </summary>
public static class AzureDeploymentReadinessChecklist
{
    public static IReadOnlyCollection<AzureDeploymentReadinessCheck> BaselineChecks { get; } = new[]
    {
        new AzureDeploymentReadinessCheck(
            "azure.environment.topology.resolved",
            "Azure environment topology resolved",
            "Topology",
            AzureDeploymentReadinessSeverity.Blocking,
            "The target environment must resolve to a known Azure topology descriptor before deployment.",
            "Verify the environment topology registry and selected deployment target profile.",
            RequiredForProduction: true),

        new AzureDeploymentReadinessCheck(
            "azure.sql.operational.store.configured",
            "SQL operational store configured",
            "Data",
            AzureDeploymentReadinessSeverity.Blocking,
            "The SQL-first operational store must be configured before real migration execution is enabled.",
            "Configure the SQL operational store connection boundary through managed identity or approved secret references.",
            RequiredForProduction: true),

        new AzureDeploymentReadinessCheck(
            "azure.identity.managed.identity.enabled",
            "Managed identity enabled",
            "Identity",
            AzureDeploymentReadinessSeverity.Error,
            "Azure hosts should use managed identity for platform resources where available.",
            "Enable managed identity for each deployed host role and grant least-privilege access.",
            RequiredForProduction: true),

        new AzureDeploymentReadinessCheck(
            "azure.telemetry.application.insights.configured",
            "Application Insights configured",
            "Observability",
            AzureDeploymentReadinessSeverity.Error,
            "Operational telemetry must be configured before sustained migration execution.",
            "Provide the telemetry connection string or managed workspace binding for the target environment.",
            RequiredForProduction: true),

        new AzureDeploymentReadinessCheck(
            "azure.queue.boundaries.configured",
            "Queue boundaries configured",
            "Runtime",
            AzureDeploymentReadinessSeverity.Error,
            "Worker and dispatcher roles require explicit queue boundaries before deployment.",
            "Verify queue names, poison queue names, and environment-specific queue prefixes.",
            RequiredForProduction: true),

        new AzureDeploymentReadinessCheck(
            "azure.operator.access.reviewed",
            "Operator access reviewed",
            "Operations",
            AzureDeploymentReadinessSeverity.Warning,
            "Operator UI/API access should be reviewed before enabling production operations.",
            "Review operator groups, admin policies, and production access boundaries.",
            RequiredForProduction: false)
    };
}
