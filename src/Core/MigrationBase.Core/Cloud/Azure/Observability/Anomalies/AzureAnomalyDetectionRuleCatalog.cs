using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Observability.Anomalies;

public sealed class AzureAnomalyDetectionRuleCatalog : IAzureAnomalyDetectionRuleCatalog
{
    private readonly IReadOnlyList<AzureAnomalyDetectionRuleDescriptor> rules;

    public AzureAnomalyDetectionRuleCatalog(IEnumerable<AzureAnomalyDetectionRuleDescriptor> rules)
    {
        this.rules = (rules ?? Array.Empty<AzureAnomalyDetectionRuleDescriptor>())
            .Where(rule => rule is not null)
            .ToArray();
    }

    public IReadOnlyList<AzureAnomalyDetectionRuleDescriptor> GetRules() => rules;
}
