using Migration.Admin.Api.Endpoints.Operational.SqlBackbone;
using Migration.Admin.Api.Registration;
using Migration.Admin.Api.Authentication;
using Migration.Admin.Api.Endpoints;
using Migration.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAdminApiSqlOperationalBackbone(builder.Configuration);

Migration.Admin.Api.Configuration.AdminApiConfigurationExtensions.ConfigureAdminApiConfiguration(builder);
Migration.Admin.Api.Configuration.AdminApiConfigurationExtensions.LogAdminApiConfiguration(builder);

Migration.Admin.Api.Registration.AdminApiOpenApiServiceCollectionExtensions.AddMigrationAdminApiOpenApi(builder.Services);
builder.Services.AddMigrationAdminApiRuntime(builder.Configuration);
builder.Services.AddOperationalStore(builder.Configuration);
builder.Services.AddMigrationAdminApiOperationalRunMirror(builder.Configuration);
builder.Services.AddMigrationAdminApiAuthentication(builder.Configuration, builder.Environment);
AdminApiCloudStartupExtensions.AddMigrationAdminApiCloudServices(builder.Services, builder.Configuration);
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
app.MapAdminEndpointDiagnostics();

var api = app.MapGroup("/api");

AdminApiEndpointStartupExtensions.MapMigrationAdminApiRouteGroupEndpoints(api);
AdminApiEndpointStartupExtensions.MapMigrationAdminApiAppLevelEndpoints(app);

app.MapSqlOperationalBackboneEndpoints();
app.Run();



