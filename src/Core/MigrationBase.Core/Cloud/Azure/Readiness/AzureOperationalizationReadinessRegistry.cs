namespace MigrationBase.Core.Cloud.Azure.Readiness;

public sealed class AzureOperationalizationReadinessRegistry : IAzureOperationalizationReadinessRegistry
{
    private static readonly AzureOperationalizationReadinessItem[] RequiredItems =
    {
        new("p5.1.runtime-topology", AzureOperationalizationReadinessCategory.RuntimeTopology, AzureOperationalizationReadinessStatus.Defined, "Azure runtime topology, environment, tenant, and promotion boundaries are defined.", "P5.1 closeout", true),
        new("p5.2.worker-stabilization", AzureOperationalizationReadinessCategory.WorkerStabilization, AzureOperationalizationReadinessStatus.Defined, "Worker lifecycle, heartbeat, lease, retry, poison, drain, and diagnostic contracts are defined.", "P5.2 closeout", true),
        new("p5.3.deployment-automation", AzureOperationalizationReadinessCategory.DeploymentAutomation, AzureOperationalizationReadinessStatus.Defined, "Deployment pipeline, environment manifests, parameters, rollback, approval, and health-check contracts are defined.", "P5.3 closeout", true),
        new("p5.4.observability", AzureOperationalizationReadinessCategory.Observability, AzureOperationalizationReadinessStatus.Defined, "Correlation, structured log, metric, health signal, alert, dashboard, and anomaly contracts are defined.", "P5.4 closeout", true),
        new("p5.5.real-migration-validation", AzureOperationalizationReadinessCategory.RealMigrationValidation, AzureOperationalizationReadinessStatus.Defined, "Execution, resumability, replay, fault-injection, throughput, audit, and large-manifest validation contracts are defined.", "P5.5 closeout", true),
        new("p5.6.production-governance", AzureOperationalizationReadinessCategory.ProductionGovernance, AzureOperationalizationReadinessStatus.Defined, "Production readiness, maintenance, emergency stop, authorization, override, and checklist governance contracts are defined.", "P5.6 closeout", true)
    };

    public IReadOnlyCollection<AzureOperationalizationReadinessItem> GetRequiredItems() => RequiredItems;
}
