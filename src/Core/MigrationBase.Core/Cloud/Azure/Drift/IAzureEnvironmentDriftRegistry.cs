namespace MigrationBase.Core.Cloud.Azure.Drift;

public interface IAzureEnvironmentDriftRegistry
{
    IReadOnlyList<AzureEnvironmentDriftDescriptor> GetExpectedDriftChecks(string environmentName);
}
