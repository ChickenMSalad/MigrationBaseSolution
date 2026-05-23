namespace MigrationBase.Core.Cloud.Azure.Drift;

public sealed class AzureEnvironmentDriftRegistry : IAzureEnvironmentDriftRegistry
{
    private readonly IReadOnlyList<AzureEnvironmentDriftDescriptor> _checks;

    public AzureEnvironmentDriftRegistry(IEnumerable<AzureEnvironmentDriftDescriptor>? checks = null)
    {
        _checks = (checks ?? Array.Empty<AzureEnvironmentDriftDescriptor>()).ToArray();
    }

    public IReadOnlyList<AzureEnvironmentDriftDescriptor> GetExpectedDriftChecks(string environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return Array.Empty<AzureEnvironmentDriftDescriptor>();
        }

        return _checks
            .Where(check => string.Equals(check.EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
