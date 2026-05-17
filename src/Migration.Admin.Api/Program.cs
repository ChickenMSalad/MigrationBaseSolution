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
builder.Services.AddCloudStoragePathResolution(builder.Configuration);
builder.Services.AddCloudBinaryStorage(builder.Configuration);
builder.Services.AddArtifactStorage();
builder.Services.AddArtifactManifestIndex();
builder.Services.AddCloudCredentialPlanning(builder.Configuration);
builder.Services.AddCloudCredentialValueProvider(builder.Configuration);
builder.Services.AddQueueDispatchProvider(builder.Configuration);
builder.Services.AddQueueReceiveProvider(builder.Configuration);
builder.Services.AddQueueFailureHandling();
builder.Services.AddQueueExecutionPlanning();
builder.Services.AddQueueExecutorCoordinator(builder.Configuration);
builder.Services.AddQueueExecutionObservability();
builder.Services.AddQueueExecutionReadiness();
builder.Services.AddAuditPersistence(builder.Configuration);
builder.Services.AddAuditEventWriter();
builder.Services.AddTelemetrySink(builder.Configuration);
builder.Services.AddTelemetryEventWriter();
builder.Services.AddOperationalReadiness();
builder.Services.AddAuthPolicyReadiness();
builder.Services.AddEndpointPolicyInventory();
builder.Services.AddCredentialAccessPolicyReadiness();

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



















































