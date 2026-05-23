namespace MigrationBase.Core.Cloud.Azure.Validation.Throughput;

public interface IAzureMigrationThroughputValidationRegistry
{
    IReadOnlyCollection<AzureMigrationThroughputValidationProfile> GetProfiles();
    AzureMigrationThroughputValidationProfile? FindProfile(string profileName);
}
