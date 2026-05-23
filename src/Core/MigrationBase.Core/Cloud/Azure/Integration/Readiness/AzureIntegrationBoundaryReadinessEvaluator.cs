namespace MigrationBase.Core.Cloud.Azure.Integration.Readiness;

public sealed class AzureIntegrationBoundaryReadinessEvaluator : IAzureIntegrationBoundaryReadinessEvaluator
{
    public AzureIntegrationBoundaryReadinessResult Evaluate(IEnumerable<AzureIntegrationBoundaryReadinessCheck> checks, ISet<string> availableConfigurationKeys, ISet<string> availableCapabilities)
    {
        ArgumentNullException.ThrowIfNull(checks);
        ArgumentNullException.ThrowIfNull(availableConfigurationKeys);
        ArgumentNullException.ThrowIfNull(availableCapabilities);

        var result = new AzureIntegrationBoundaryReadinessResult();

        foreach (var check in checks)
        {
            result.Checks.Add(check);

            foreach (var key in check.RequiredConfigurationKeys.Where(key => !availableConfigurationKeys.Contains(key)))
            {
                result.Findings.Add(new AzureIntegrationBoundaryReadinessFinding
                {
                    Name = check.Name,
                    Boundary = check.Boundary,
                    Level = check.Level,
                    Message = $"Missing required configuration key '{key}' for Azure integration boundary '{check.Boundary}'."
                });
            }

            foreach (var capability in check.RequiredCapabilities.Where(capability => !availableCapabilities.Contains(capability)))
            {
                result.Findings.Add(new AzureIntegrationBoundaryReadinessFinding
                {
                    Name = check.Name,
                    Boundary = check.Boundary,
                    Level = check.Level,
                    Message = $"Missing required capability '{capability}' for Azure integration boundary '{check.Boundary}'."
                });
            }
        }

        return result;
    }
}
