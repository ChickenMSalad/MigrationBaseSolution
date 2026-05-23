namespace MigrationBase.Core.Cloud.Azure.Integration.Readiness;

public enum AzureIntegrationBoundaryReadinessLevel
{
    Informational = 0,
    Recommended = 1,
    Required = 2,
    Blocking = 3
}
