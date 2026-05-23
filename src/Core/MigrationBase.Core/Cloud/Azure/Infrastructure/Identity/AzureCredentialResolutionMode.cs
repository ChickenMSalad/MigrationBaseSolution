namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Identity;

public enum AzureCredentialResolutionMode
{
    Unspecified = 0,
    ManagedIdentity = 1,
    WorkloadIdentity = 2,
    DeveloperCredential = 3,
    ConnectionString = 4,
    Disabled = 5
}
