namespace MigrationBase.Core.Cloud.Azure.Validation.Throughput;

public sealed class AzureMigrationThroughputValidationRegistry : IAzureMigrationThroughputValidationRegistry
{
    private readonly IReadOnlyCollection<AzureMigrationThroughputValidationProfile> profiles;

    public AzureMigrationThroughputValidationRegistry(IEnumerable<AzureMigrationThroughputValidationProfile> profiles)
    {
        this.profiles = profiles?.ToArray() ?? Array.Empty<AzureMigrationThroughputValidationProfile>();
    }

    public IReadOnlyCollection<AzureMigrationThroughputValidationProfile> GetProfiles() => profiles;

    public AzureMigrationThroughputValidationProfile? FindProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return null;
        }

        return profiles.FirstOrDefault(profile => string.Equals(profile.ProfileName, profileName, StringComparison.OrdinalIgnoreCase));
    }
}
