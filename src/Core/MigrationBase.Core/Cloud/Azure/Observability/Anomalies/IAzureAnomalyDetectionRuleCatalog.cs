using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Observability.Anomalies;

public interface IAzureAnomalyDetectionRuleCatalog
{
    IReadOnlyList<AzureAnomalyDetectionRuleDescriptor> GetRules();
}
