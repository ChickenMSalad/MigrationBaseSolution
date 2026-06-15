using Migration.Admin.Api.Endpoints.Operational.Dashboard;
using Migration.Admin.Api.Operational.Execution;
using Migration.Admin.Api.Operational.Events;
using Migration.Admin.Api.Operational.SqlMetrics;
using Migration.Admin.Api.Endpoints.Operational;
using Migration.Admin.Api.Endpoints.Operational.Credentials;
using Migration.Admin.Api.Endpoints.Operational.SqlBackbone;
using Migration.Admin.Api.Registration;
using Migration.Admin.Api.Authentication;
using Migration.Admin.Api.Endpoints;
using Migration.Infrastructure.Sql.Registration;
using Migration.Admin.Api.Endpoints.Operational.Connectors;
using Migration.Admin.Api.Operational;
using Migration.Application.Registration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAdminApiOperationalRuntimeReadiness(builder.Configuration);
builder.Services.AddAdminApiOperationalRunCoordinator(builder.Configuration);
builder.Services.AddSqlOperationalWorkItemQueue();
builder.Services.AddAdminApiSqlOperationalBackbone(builder.Configuration);

Migration.Admin.Api.Configuration.AdminApiConfigurationExtensions.ConfigureAdminApiConfiguration(builder);
Migration.Admin.Api.Configuration.AdminApiConfigurationExtensions.LogAdminApiConfiguration(builder);

AdminApiOpenApiServiceCollectionExtensions.AddMigrationAdminApiOpenApi(builder.Services);
builder.Services.AddMigrationAdminApiRuntime(builder.Configuration);

builder.Services.AddSqlOperationalStore(builder.Configuration);

builder.Services.AddMigrationAdminApiOperationalRunMirror(builder.Configuration);
builder.Services.AddMigrationAdminApiAuthentication(builder.Configuration, builder.Environment);
AdminApiCloudStartupExtensions.AddMigrationAdminApiCloudServices(builder.Services, builder.Configuration);
builder.Services.AddAdminApiConnectorCredentialVault();
builder.Services.Configure<OperationalEventRetentionOptions>(builder.Configuration.GetSection(OperationalEventRetentionOptions.SectionName));
builder.Services.Configure<OperationalEventSnapshotRecorderOptions>(builder.Configuration.GetSection(OperationalEventSnapshotRecorderOptions.SectionName));
builder.Services.AddScoped<IOperationalEventRetentionService, SqlOperationalEventRetentionService>();
builder.Services.AddScoped<ISqlOperationalMetricsReader, SqlOperationalMetricsReader>();
builder.Services.AddHostedService<OperationalEventRetentionWorker>();
builder.Services.AddHostedService<OperationalEventSnapshotRecorderService>();
builder.Services.AddOperationalAdminServices(builder.Configuration);
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Migration Admin API v1");
    options.DocumentTitle = "Migration Admin API";
});

app.UseMigrationAdminApiAuthenticationState();
app.MapAdminSecurityStatusEndpoints();

Migration.Admin.Api.Endpoints.AdminSystemEndpointExtensions.MapAdminSystemEndpoints(app);

app.MapOperationalHealthEndpoints();
app.MapAdminEndpointDiagnostics();

var api = app.MapGroup("/api"); api.MapOperationalDispatchEndpoints(); api.MapOperationalDispatcherEndpoints(); api.MapOperationalDispatcherDashboardEndpoints(); api.MapOperationalDispatcherDiagnosticsEndpoints(); api.MapOperationalDispatcherExecutionHistoryEndpoints(); api.MapOperationalDispatcherExecutionHistoryQueryEndpoints(); api.MapOperationalDispatcherExecutionHistoryReadinessEndpoints(); api.MapOperationalDispatcherExecutionHistoryRetentionEndpoints(); api.MapOperationalDispatcherExecutionMetricsEndpoints();

AdminApiEndpointStartupExtensions.MapMigrationAdminApiRouteGroupEndpoints(api);
AdminApiEndpointStartupExtensions.MapMigrationAdminApiAppLevelEndpoints(app);

app.MapSqlOperationalWorkItemQueueEndpoints();
app.MapSqlOperationalRunCoordinatorEndpoints();
app.MapSqlOperationalRuntimeReadinessEndpoints();
app.MapSqlOperationalRuntimeDashboardEndpoints();
app.MapSqlOperationalRuntimeDashboardDetailEndpoints();
app.MapOperationalConnectorCredentialVaultEndpoints();

app.MapOperationalConnectorExecutionProfileEndpoints();
app.MapMigrationOperationalEndpoints();


app.Run();



















































































