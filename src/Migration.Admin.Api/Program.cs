using Migration.ControlPlane.Auth;
using Migration.ControlPlane.Operations;
using Migration.ControlPlane.Telemetry;
using Migration.ControlPlane.Audit;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Credentials;
using Migration.ControlPlane.Storage;
using Migration.Admin.Api.Endpoints;
using Migration.Admin.Api.Registration;
using Migration.Admin.Api.Authentication;

var builder = WebApplication.CreateBuilder(args);

Migration.Admin.Api.Configuration.AdminApiConfigurationExtensions.ConfigureAdminApiConfiguration(builder);
Migration.Admin.Api.Configuration.AdminApiConfigurationExtensions.LogAdminApiConfiguration(builder);

Migration.Admin.Api.Registration.AdminApiOpenApiServiceCollectionExtensions.AddMigrationAdminApiOpenApi(builder.Services);
builder.Services.AddMigrationAdminApiRuntime(builder.Configuration);
builder.Services.AddMigrationAdminApiAuthentication(builder.Configuration, builder.Environment);
AddMigrationAdminApiCloudServices(builder.Services, builder.Configuration);
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Migration Admin API v1");
    options.DocumentTitle = "Migration Admin API";
});

app.UseMigrationAdminApiAuthenticationState();

Migration.Admin.Api.Endpoints.AdminSystemEndpointExtensions.MapAdminSystemEndpoints(app);

app.MapOperationalHealthEndpoints();

var api = app.MapGroup("/api");

api.MapRunMonitoringEndpoints();
api.MapCredentialEndpoints();
api.MapProjectArtifactBindingEndpoints();
api.MapProjectCredentialBindingEndpoints();
api.MapPreflightEndpoints();
api.MapProjectEndpoints();
api.MapRunEndpoints();
api.MapRunExecutionPolicyEndpoints();
MapMigrationAdminApiCloudEndpoints(api);
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

// These extensions include their /api route prefix internally. Keep them on app, not on the /api group.
app.MapArtifactEndpoints();
app.MapControlPlaneDeleteEndpoints();
app.MapMappingBuilderEndpoints();
app.MapManifestBuilderEndpoints();
app.MapTaxonomyBuilderEndpoints();

app.Run();

static void AddMigrationAdminApiCloudServices(
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

static void MapMigrationAdminApiCloudEndpoints(
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
