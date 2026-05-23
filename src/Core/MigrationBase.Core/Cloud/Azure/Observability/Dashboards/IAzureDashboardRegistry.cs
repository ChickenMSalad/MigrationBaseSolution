using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Observability.Dashboards;

public interface IAzureDashboardRegistry
{
    IReadOnlyList<AzureDashboardDescriptor> GetDashboards();

    AzureDashboardValidationResult Validate();
}
