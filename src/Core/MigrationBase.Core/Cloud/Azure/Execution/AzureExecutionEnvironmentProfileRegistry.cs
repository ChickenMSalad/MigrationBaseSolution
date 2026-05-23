namespace MigrationBase.Core.Cloud.Azure.Execution;

public sealed class AzureExecutionEnvironmentProfileRegistry : IAzureExecutionEnvironmentProfileRegistry
{
    private readonly IReadOnlyList<AzureExecutionEnvironmentProfile> _profiles;

    public AzureExecutionEnvironmentProfileRegistry(IEnumerable<AzureExecutionEnvironmentProfile> profiles)
    {
        _profiles = profiles?.ToArray() ?? Array.Empty<AzureExecutionEnvironmentProfile>();
    }

    public IReadOnlyList<AzureExecutionEnvironmentProfile> GetProfiles() => _profiles;

    public AzureExecutionEnvironmentProfile? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _profiles.FirstOrDefault(profile => string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
