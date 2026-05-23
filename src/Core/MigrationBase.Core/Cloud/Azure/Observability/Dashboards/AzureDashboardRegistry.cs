using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Observability.Dashboards;

public sealed class AzureDashboardRegistry : IAzureDashboardRegistry
{
    private readonly IReadOnlyList<AzureDashboardDescriptor> dashboards;

    public AzureDashboardRegistry(IEnumerable<AzureDashboardDescriptor> dashboards)
    {
        this.dashboards = dashboards?.ToArray() ?? Array.Empty<AzureDashboardDescriptor>();
    }

    public IReadOnlyList<AzureDashboardDescriptor> GetDashboards() => dashboards;

    public AzureDashboardValidationResult Validate()
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dashboard in dashboards)
        {
            if (string.IsNullOrWhiteSpace(dashboard.DashboardId))
            {
                errors.Add("DashboardId is required.");
                continue;
            }

            if (!ids.Add(dashboard.DashboardId))
            {
                errors.Add($"Duplicate dashboard id '{dashboard.DashboardId}'.");
            }

            if (string.IsNullOrWhiteSpace(dashboard.DisplayName))
            {
                errors.Add($"Dashboard '{dashboard.DashboardId}' requires a display name.");
            }

            if (dashboard.Panels.Count == 0)
            {
                errors.Add($"Dashboard '{dashboard.DashboardId}' should define at least one panel.");
            }
        }

        return errors.Count == 0
            ? AzureDashboardValidationResult.Success()
            : AzureDashboardValidationResult.Failed(errors);
    }
}
