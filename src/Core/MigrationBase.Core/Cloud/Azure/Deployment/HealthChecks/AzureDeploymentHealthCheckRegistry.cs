namespace MigrationBase.Core.Cloud.Azure.Deployment.HealthChecks;

public sealed class AzureDeploymentHealthCheckRegistry : IAzureDeploymentHealthCheckRegistry
{
    private readonly IReadOnlyList<AzureDeploymentHealthCheckDescriptor> _checks;

    public AzureDeploymentHealthCheckRegistry(IEnumerable<AzureDeploymentHealthCheckDescriptor> checks)
    {
        _checks = checks?.ToArray() ?? Array.Empty<AzureDeploymentHealthCheckDescriptor>();
    }

    public IReadOnlyList<AzureDeploymentHealthCheckDescriptor> GetAll() => _checks;

    public IReadOnlyList<AzureDeploymentHealthCheckDescriptor> GetRequiredForPromotion() =>
        _checks.Where(check => check.IsRequiredForPromotion).ToArray();
}
