using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Endpoints.Operational.Audit;
using Migration.Admin.Api.Endpoints.Operational.Capacity;
using Migration.Admin.Api.Endpoints.Operational.Connectors;
using Migration.Admin.Api.Endpoints.Operational.Cost;
using Migration.Admin.Api.Endpoints.Operational.Notifications;
using Migration.Admin.Api.Endpoints.Operational.SlaSlo;
using Migration.Admin.Api.Endpoints.Operational.SqlBackbone;
using Migration.Admin.Api.Endpoints.Operational.Workers;

namespace Migration.Admin.Api.Endpoints.Operational;

public static class MigrationOperationalEndpointCompositionExtensions
{
    public static IEndpointRouteBuilder MapMigrationOperationalEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSqlOperationalBackboneEndpoints();
        endpoints.MapOperationalWorkerTelemetryEndpoints();
        endpoints.MapOperationalConnectorConfigurationEndpoints();
        endpoints.MapOperationalAuditTrailEndpoints();
        endpoints.MapOperationalNotificationEndpoints();
        endpoints.MapOperationalSlaSloEndpoints();
        endpoints.MapOperationalCapacityEndpoints();
        endpoints.MapOperationalCostAnalyticsEndpoints();

        return endpoints;
    }
}
