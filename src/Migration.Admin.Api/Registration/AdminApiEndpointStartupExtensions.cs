using Migration.Admin.Api.Endpoints;

namespace Migration.Admin.Api.Registration;

public static class AdminApiEndpointStartupExtensions
{
    public static void MapMigrationAdminApiRouteGroupEndpoints(
        RouteGroupBuilder api)
    {
        api.MapOperationalDispatchEndpoints();
        api.MapOperationalDispatcherEndpoints();
        api.MapOperationalDispatcherDiagnosticsEndpoints();
        api.MapOperationalDispatcherExecutionHistoryEndpoints();
        api.MapOperationalDispatcherExecutionHistoryReadinessEndpoints();
        api.MapOperationalDispatcherExecutionMetricsEndpoints();
        api.MapOperationalDispatcherExecutionHistoryQueryEndpoints();
        api.MapOperationalDispatcherExecutionHistoryRetentionEndpoints();
        api.MapOperationalDispatcherDashboardEndpoints();
        api.MapOperationalMirrorDiagnosticsEndpoints();
        api.MapOperationalSqlSchemaDiagnosticsEndpoints();
        api.MapOperationalMirrorReadEndpoints();
        api.MapOperationalRunStatusProjectionEndpoints();
        api.MapOperationalWorkItemLeaseEndpoints();
        api.MapOperationalWorkItemLeaseExpirationEndpoints();
        api.MapOperationalMetricsEndpoints();
        api.MapOperationalRetentionEndpoints();
        api.MapOperationalRunControlEndpoints();
        api.MapOperationalRunStatusReconciliationEndpoints();
        api.MapOperationalRunCompletionFinalizationEndpoints();
        api.MapOperationalRunDashboardEndpoints();
        api.MapOperationalRunTimelineEndpoints();
        api.MapOperationalRunTimelineQueryEndpoints();
        api.MapOperationalRunTimelineMetricsEndpoints();
        api.MapOperationalRunTimelineDashboardEndpoints();
        api.MapOperationalRunTimelineSearchEndpoints();
        api.MapOperationalRunTimelineCatalogEndpoints();
        api.MapOperationalRunTimelineGlobalCatalogEndpoints();
        api.MapOperationalGlobalActivityFeedEndpoints();
        api.MapOperationalGlobalActivityQueryEndpoints();
        api.MapOperationalGlobalActivityMetricsEndpoints();
        api.MapOperationalGlobalActivityDashboardEndpoints();
        api.MapOperationalGlobalFailureEndpoints();
        api.MapOperationalGlobalFailureMetricsEndpoints();
        api.MapOperationalGlobalFailureDashboardEndpoints();
        api.MapOperationalGlobalFailureQueryEndpoints();
        api.MapOperationalGlobalFailureCatalogEndpoints();
        api.MapOperationalGlobalFailureSystemPairMetricsEndpoints();
        api.MapOperationalGlobalFailureRunStatusMetricsEndpoints();
        api.MapOperationalRunFailureFinalizationEndpoints();
        api.MapOperationalRunAutoFinalizationEndpoints();
        ArgumentNullException.ThrowIfNull(api);

        api.MapRunMonitoringEndpoints();
        api.MapCredentialEndpoints();
        api.MapProjectArtifactBindingEndpoints();
        api.MapProjectCredentialBindingEndpoints();
        api.MapPreflightEndpoints();
        api.MapProjectEndpoints();
        api.MapRunEndpoints();
        api.MapRunExecutionPolicyEndpoints();

        AdminApiCloudStartupExtensions.MapMigrationAdminApiCloudEndpoints(api);

        api.MapCloudConfigurationAuditEndpoints();
        api.MapDeploymentProfileEndpoints();
        api.MapCloudReadinessEndpoints();

        api.MapQueueProviderPlanEndpoints();
        api.MapQueueContractDiagnosticsEndpoints();
        api.MapQueueIdempotencyEndpoints();
        api.MapQueueDispatchDiagnosticsEndpoints();
        api.MapQueueReceiveDiagnosticsEndpoints();
        api.MapQueueWorkerLoopDiagnosticsEndpoints();
        api.MapQueuePoisonHandlingEndpoints();
        api.MapQueueFailureArtifactEndpoints();
        api.MapQueueFailureHandlerEndpoints();
        api.MapQueueExecutionPlannerEndpoints();
        api.MapQueueExecutorCoordinatorEndpoints();
        api.MapQueueExecutionObservabilityEndpoints();
        api.MapQueueExecutionReadinessEndpoints();

        api.MapArtifactStoragePlanEndpoints();
        api.MapCredentialProviderPlanEndpoints();
        api.MapWorkspaceContextEndpoints();
        api.MapWorkspaceStoragePlanEndpoints();
        api.MapConnectorCatalogEndpoints();
        api.MapConnectorCapabilityEndpoints();
    }

    public static void MapMigrationAdminApiAppLevelEndpoints(
        WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // These extensions include their /api route prefix internally.
        // Keep them on app, not on the /api route group.
        app.MapArtifactEndpoints();
        app.MapControlPlaneDeleteEndpoints();
        app.MapMappingBuilderEndpoints();
        app.MapManifestBuilderEndpoints();
        app.MapTaxonomyBuilderEndpoints();
    }
}
