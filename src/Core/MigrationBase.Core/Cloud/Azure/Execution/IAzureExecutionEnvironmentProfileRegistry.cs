namespace MigrationBase.Core.Cloud.Azure.Execution;

public interface IAzureExecutionEnvironmentProfileRegistry
{
    IReadOnlyList<AzureExecutionEnvironmentProfile> GetProfiles();

    AzureExecutionEnvironmentProfile? FindByName(string name);
}
