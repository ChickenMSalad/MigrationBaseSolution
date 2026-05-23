namespace MigrationBase.Core.Cloud.Azure.Workers.Concurrency;

public sealed class AzureWorkerConcurrencyProfileRegistry : IAzureWorkerConcurrencyProfileRegistry
{
    private readonly IReadOnlyCollection<AzureWorkerConcurrencyProfile> _profiles;

    public AzureWorkerConcurrencyProfileRegistry(IEnumerable<AzureWorkerConcurrencyProfile> profiles)
    {
        _profiles = profiles?.ToArray() ?? Array.Empty<AzureWorkerConcurrencyProfile>();
    }

    public AzureWorkerConcurrencyProfile? FindByName(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return null;
        }

        return _profiles.FirstOrDefault(profile => string.Equals(profile.Name, profileName, StringComparison.OrdinalIgnoreCase));
    }

    public AzureWorkerConcurrencyProfile? FindForWorkerRole(string workerRole)
    {
        if (string.IsNullOrWhiteSpace(workerRole))
        {
            return null;
        }

        return _profiles.FirstOrDefault(profile => string.Equals(profile.WorkerRole, workerRole, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyCollection<AzureWorkerConcurrencyProfile> ListProfiles() => _profiles;
}
