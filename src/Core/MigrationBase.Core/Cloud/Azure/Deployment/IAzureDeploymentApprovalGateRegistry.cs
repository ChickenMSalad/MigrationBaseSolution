namespace MigrationBase.Core.Cloud.Azure.Deployment;

public interface IAzureDeploymentApprovalGateRegistry
{
    IReadOnlyList<AzureDeploymentApprovalGate> GetApprovalGates();
    AzureDeploymentApprovalGate? FindApprovalGate(string gateId);
    IReadOnlyList<AzureDeploymentApprovalGate> FindBlockingGates(string environmentName, string deploymentRing);
}
