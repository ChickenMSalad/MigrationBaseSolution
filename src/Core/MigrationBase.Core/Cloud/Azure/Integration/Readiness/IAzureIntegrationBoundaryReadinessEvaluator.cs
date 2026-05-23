namespace MigrationBase.Core.Cloud.Azure.Integration.Readiness;

public interface IAzureIntegrationBoundaryReadinessEvaluator
{
    AzureIntegrationBoundaryReadinessResult Evaluate(IEnumerable<AzureIntegrationBoundaryReadinessCheck> checks, ISet<string> availableConfigurationKeys, ISet<string> availableCapabilities);
}
