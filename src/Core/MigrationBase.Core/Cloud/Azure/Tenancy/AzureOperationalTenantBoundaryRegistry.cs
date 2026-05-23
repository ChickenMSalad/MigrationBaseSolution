namespace MigrationBase.Core.Cloud.Azure.Tenancy;

public sealed class AzureOperationalTenantBoundaryRegistry : IAzureOperationalTenantBoundaryRegistry
{
    private readonly IReadOnlyDictionary<string, AzureOperationalTenantBoundary> _boundaries;

    public AzureOperationalTenantBoundaryRegistry(IEnumerable<AzureOperationalTenantBoundary> boundaries)
    {
        if (boundaries is null)
        {
            throw new ArgumentNullException(nameof(boundaries));
        }

        _boundaries = boundaries
            .Where(boundary => boundary is not null && !string.IsNullOrWhiteSpace(boundary.Name))
            .GroupBy(boundary => boundary.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<AzureOperationalTenantBoundary> GetAll()
    {
        return _boundaries.Values.ToArray();
    }

    public AzureOperationalTenantBoundary? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _boundaries.TryGetValue(name, out var boundary) ? boundary : null;
    }

    public AzureOperationalTenantBoundaryValidationResult Validate(AzureOperationalTenantBoundary boundary)
    {
        if (boundary is null)
        {
            return AzureOperationalTenantBoundaryValidationResult.Failed("Tenant boundary is required.");
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(boundary.Name)) errors.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(boundary.TenantId)) errors.Add("TenantId is required.");
        if (string.IsNullOrWhiteSpace(boundary.SubscriptionId)) errors.Add("SubscriptionId is required.");
        if (string.IsNullOrWhiteSpace(boundary.EnvironmentName)) errors.Add("EnvironmentName is required.");
        if (string.IsNullOrWhiteSpace(boundary.DeploymentRing)) errors.Add("DeploymentRing is required.");

        if (boundary.AllowsProductionWorkloads && !boundary.RequiresManagedIdentity)
        {
            errors.Add("Production workload boundaries must require managed identity.");
        }

        return errors.Count == 0
            ? AzureOperationalTenantBoundaryValidationResult.Passed()
            : AzureOperationalTenantBoundaryValidationResult.Failed(errors.ToArray());
    }
}
