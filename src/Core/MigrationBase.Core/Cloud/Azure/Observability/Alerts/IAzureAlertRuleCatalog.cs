using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Observability.Alerts;

public interface IAzureAlertRuleCatalog
{
    IReadOnlyList<AzureAlertRuleDescriptor> GetAll();
    AzureAlertRuleDescriptor? FindByName(string name);
}
