using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Endpoints.Operational.Audit;
using Migration.Admin.Api.Endpoints.Operational.Capacity;
using Migration.Admin.Api.Endpoints.Operational.Connectors;
using Migration.Admin.Api.Endpoints.Operational.Execution;
using Migration.Admin.Api.Endpoints.Operational.Events;
using Migration.Admin.Api.Endpoints.Operational.CommandCenter;
using Migration.Admin.Api.Endpoints.Operational.Cost;
using Migration.Admin.Api.Endpoints.Operational.Notifications;
using Migration.Admin.Api.Endpoints.Operational.SlaSlo;
using Migration.Admin.Api.Endpoints.Operational.SqlHealth;
using Migration.Admin.Api.Endpoints.Operational.SqlBackbone;
using Migration.Admin.Api.Endpoints.Operational.Workers;

namespace Migration.Admin.Api.Endpoints.Operational;

public static class MigrationOperationalEndpointCompositionExtensions
{
    public static IEndpointRouteBuilder MapMigrationOperationalEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOperationalEventExportEndpoints();
        endpoints.MapOperationalEventQueryEndpoints();
        endpoints.MapOperationalEventRetentionEndpoints();
        endpoints.MapExecutionWorkerTelemetryEndpoints();
        endpoints.MapExecutionControlEndpoints();
        endpoints.MapExecutionWorkItemQueueEndpoints();
        endpoints.MapExecutionPlanEndpoints();
        endpoints.MapExecutionLifecycleEndpoints();
        endpoints.MapExecutionSessionEndpoints();
        endpoints.MapOperationalEventEndpoints();
        endpoints.MapOperationalCommandCenterEndpoints();
        endpoints.MapOperationalSqlHealthEndpoints();
        endpoints.MapSqlOperationalBackboneEndpoints();
        endpoints.MapOperationalWorkerTelemetryEndpoints();
        endpoints.MapOperationalConnectorConfigurationEndpoints();
        endpoints.MapOperationalAuditTrailEndpoints();
        endpoints.MapOperationalNotificationEndpoints();
        endpoints.MapOperationalSlaSloEndpoints();
        endpoints.MapOperationalCapacityEndpoints();
        endpoints.MapOperationalCostAnalyticsEndpoints();

        endpoints.MapExecutionReplayEndpoints();
        return endpoints;
    }
}



































