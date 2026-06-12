using Migration.Admin.Api.Endpoints;
using Migration.ControlPlane.Auth;
using Migration.ControlPlane.Audit;
using Migration.ControlPlane.Credentials;
using Migration.ControlPlane.Operations;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Storage;
using Migration.ControlPlane.Telemetry;
namespace Migration.Admin.Api.Registration;

public static class AdminApiCloudStartupExtensions
{
    public static void AddMigrationAdminApiCloudServices(
        Microsoft.Extensions.DependencyInjection.IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddCloudStoragePathResolution(configuration);
            services.AddCloudBinaryStorage(configuration);
            services.AddArtifactStorage();
            services.AddArtifactManifestIndex();
            services.AddCloudCredentialPlanning(configuration);
            services.AddCloudCredentialValueProvider(configuration);
            services.AddQueueDispatchProvider(configuration);
            services.AddQueueReceiveProvider(configuration);
            services.AddQueueFailureHandling();
            services.AddQueueExecutionPlanning();
            services.AddQueueExecutorCoordinator(configuration);
            services.AddQueueExecutionObservability();
            services.AddQueueExecutionReadiness();
            services.AddAuditPersistence(configuration);
            services.AddAuditEventWriter();
            services.AddTelemetrySink(configuration);
            services.AddTelemetryEventWriter();
            services.AddOperationalReadiness();
            services.AddAuthPolicyReadiness();
            services.AddEndpointPolicyInventory();
            services.AddCredentialAccessPolicyReadiness();
            services.AddAuthEnforcementDiagnostics();
            services.AddProductionSafetyGates();
            services.AddOperationalMode();
            services.AddQueueExecutionGovernance();
            services.AddP2ReadinessReport();

    }

    public static void MapMigrationAdminApiCloudEndpoints(
        Microsoft.AspNetCore.Routing.RouteGroupBuilder api)
    {
        api.MapCloudPlatformEndpoints();
            api.MapCloudCredentialDiagnosticsEndpoints();
            api.MapCloudCredentialValueProbeEndpoints();
            api.MapCloudStoragePlanEndpoints();
            api.MapCloudBinaryStorageProbeEndpoints();
            api.MapAzureBlobStorageDiagnosticsEndpoints();
            api.MapArtifactStorageProbeEndpoints();
            api.MapArtifactManifestIndexEndpoints();
            api.MapArtifactStorageBridgeEndpoints();
            api.MapAuthorizationPolicyPlanEndpoints();
            api.MapAuthenticationConfigurationEndpoints();
            api.MapAuditEventContractEndpoints();
            api.MapAuditPersistenceEndpoints();
            api.MapAuditArtifactPersistenceEndpoints();
            api.MapAuditEventWriterEndpoints();
            api.MapQueueAuditEventEndpoints();
            api.MapCloudOperationAuditEndpoints();
            api.MapTelemetryCorrelationEndpoints();
            api.MapTelemetrySinkEndpoints();
            api.MapTelemetryEventWriterEndpoints();
            api.MapQueueTelemetryEventEndpoints();
            api.MapCloudOperationTelemetryEndpoints();
            api.MapOperationalReadinessEndpoints();
            api.MapAuthPolicyReadinessEndpoints();
            api.MapEndpointPolicyInventoryEndpoints();
            api.MapCredentialAccessPolicyReadinessEndpoints();
            api.MapAuthEnforcementDiagnosticsEndpoints();
            api.MapProductionSafetyGateEndpoints();
            api.MapOperationalModeEndpoints();
            api.MapQueueExecutionGovernanceEndpoints();
            api.MapP2ReadinessReportEndpoints();

    }
}

