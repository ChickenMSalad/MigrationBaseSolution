namespace MigrationBase.Core.Cloud.Azure.Governance;

public interface IAzureProductionReadinessGateRegistry
{
    IReadOnlyCollection<AzureProductionReadinessGate> GetGates();

    AzureProductionReadinessGate? TryGetGate(string gateId);
}
