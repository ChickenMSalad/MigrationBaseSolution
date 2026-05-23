using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Observability.Alerts;

public sealed class AzureAlertRuleCatalog : IAzureAlertRuleCatalog
{
    private readonly IReadOnlyList<AzureAlertRuleDescriptor> _rules;

    public AzureAlertRuleCatalog(IEnumerable<AzureAlertRuleDescriptor> rules)
    {
        _rules = (rules ?? Array.Empty<AzureAlertRuleDescriptor>())
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Name))
            .GroupBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    public IReadOnlyList<AzureAlertRuleDescriptor> GetAll() => _rules;

    public AzureAlertRuleDescriptor? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _rules.FirstOrDefault(rule => string.Equals(rule.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
