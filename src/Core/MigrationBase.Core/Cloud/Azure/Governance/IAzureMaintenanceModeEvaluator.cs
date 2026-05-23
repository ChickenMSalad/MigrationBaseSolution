namespace MigrationBase.Core.Cloud.Azure.Governance;

public interface IAzureMaintenanceModeEvaluator
{
    AzureMaintenanceModeDecision EvaluateNewRunAdmission(
        string environmentName,
        string? tenantKey,
        IReadOnlyCollection<AzureMaintenanceModeDescriptor> maintenanceModes,
        IReadOnlyCollection<AzureOperationalFreezeDescriptor> freezes);

    AzureMaintenanceModeDecision EvaluateWorkItemAdmission(
        string environmentName,
        string? hostRole,
        string? queueName,
        IReadOnlyCollection<AzureMaintenanceModeDescriptor> maintenanceModes,
        IReadOnlyCollection<AzureOperationalFreezeDescriptor> freezes);

    AzureMaintenanceModeDecision EvaluateDeploymentPromotion(
        string environmentName,
        IReadOnlyCollection<AzureOperationalFreezeDescriptor> freezes);
}
