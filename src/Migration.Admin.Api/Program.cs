using Migration.Admin.Api.Endpoints;
using Migration.Admin.Api.Registration;

var builder = WebApplication.CreateBuilder(args);

Migration.Admin.Api.Configuration.AdminApiConfigurationExtensions.ConfigureAdminApiConfiguration(builder);
Migration.Admin.Api.Configuration.AdminApiConfigurationExtensions.LogAdminApiConfiguration(builder);

Migration.Admin.Api.Registration.AdminApiOpenApiServiceCollectionExtensions.AddMigrationAdminApiOpenApi(builder.Services);
builder.Services.AddMigrationAdminApiRuntime(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Migration Admin API v1");
    options.DocumentTitle = "Migration Admin API";
});

Migration.Admin.Api.Endpoints.AdminSystemEndpointExtensions.MapAdminSystemEndpoints(app);

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
api.MapCloudReadinessEndpoints();
api.MapQueueProviderPlanEndpoints();
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









