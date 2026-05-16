using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Migration.Admin.Api.Endpoints;
using Migration.Admin.Api.Registration;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Registration;
using Migration.ControlPlane.Services;
using Migration.GenericRuntime.Registration;
using Migration.Infrastructure.Taxonomy;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables(prefix: "MIGRATION_");

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ControlPlane:StorageRoot = {builder.Configuration["ControlPlane:StorageRoot"]}");
Console.WriteLine($"MigrationRunQueue:Provider = {builder.Configuration["MigrationRunQueue:Provider"]}");
Console.WriteLine($"MigrationRunQueue:QueueName = {builder.Configuration["MigrationRunQueue:QueueName"]}");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Migration Admin API",
        Version = "v1",
        Description = "Control-plane API for connector discovery, migration project setup, preflight requests, run queueing, and run status."
    });
});

builder.Services.AddMigrationAdminApiRuntime(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Migration Admin API v1");
    options.DocumentTitle = "Migration Admin API";
});

app.MapGet("/", () => Results.Redirect("/swagger"))
    .WithName("Root")
    .ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new
    {
        status = "ok",
        service = "Migration.Admin.Api",
        utc = DateTimeOffset.UtcNow
    }))
    .WithName("Health")
    .WithTags("Health")
    .WithSummary("Returns API health information.");

var api = app.MapGroup("/api");

api.MapRunMonitoringEndpoints();
api.MapCredentialEndpoints();
api.MapProjectArtifactBindingEndpoints();
api.MapProjectCredentialBindingEndpoints();
api.MapPreflightEndpoints();
api.MapProjectEndpoints();
api.MapRunEndpoints();
api.MapConnectorCatalogEndpoints();

app.MapGet("/debug/config", (IConfiguration configuration, IWebHostEnvironment env) => Results.Json(new
{
    Environment = env.EnvironmentName,
    ContentRoot = env.ContentRootPath,
    ControlPlaneStorageRoot = configuration["ControlPlane:StorageRoot"],
    QueueProvider = configuration["MigrationRunQueue:Provider"],
    QueueName = configuration["MigrationRunQueue:QueueName"]
}));

// These extensions include their /api route prefix internally. Keep them on app, not on the /api group.
app.MapArtifactEndpoints();
app.MapControlPlaneDeleteEndpoints();
app.MapMappingBuilderEndpoints();
app.MapManifestBuilderEndpoints();
app.MapTaxonomyBuilderEndpoints();

app.Run();
