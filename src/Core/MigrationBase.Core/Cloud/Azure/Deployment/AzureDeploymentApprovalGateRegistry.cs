namespace MigrationBase.Core.Cloud.Azure.Deployment;

public sealed class AzureDeploymentApprovalGateRegistry : IAzureDeploymentApprovalGateRegistry
{
    private readonly IReadOnlyList<AzureDeploymentApprovalGate> _approvalGates;

    public AzureDeploymentApprovalGateRegistry(IEnumerable<AzureDeploymentApprovalGate> approvalGates)
    {
        _approvalGates = approvalGates?.ToArray() ?? Array.Empty<AzureDeploymentApprovalGate>();
    }

    public IReadOnlyList<AzureDeploymentApprovalGate> GetApprovalGates() => _approvalGates;

    public AzureDeploymentApprovalGate? FindApprovalGate(string gateId)
    {
        if (string.IsNullOrWhiteSpace(gateId))
        {
            return null;
        }

        return _approvalGates.FirstOrDefault(gate => string.Equals(gate.GateId, gateId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<AzureDeploymentApprovalGate> FindBlockingGates(string environmentName, string deploymentRing)
    {
        if (string.IsNullOrWhiteSpace(environmentName) || string.IsNullOrWhiteSpace(deploymentRing))
        {
            return Array.Empty<AzureDeploymentApprovalGate>();
        }

        return _approvalGates
            .Where(gate => gate.BlocksDeployment)
            .Where(gate => string.Equals(gate.EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase))
            .Where(gate => string.Equals(gate.DeploymentRing, deploymentRing, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
