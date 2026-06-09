namespace Migration.Admin.Api.Endpoints;

public static class SystemEndpointExtensions
{
    public static WebApplication MapAdminSystemEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

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

        app.MapGet("/debug/config", (IConfiguration configuration, IWebHostEnvironment env) => Results.Json(new
        {
            Environment = env.EnvironmentName,
            ContentRoot = env.ContentRootPath,
            ControlPlaneStorageRoot = configuration["ControlPlane:StorageRoot"],
            QueueProvider = configuration["MigrationRunQueue:Provider"],
            QueueName = configuration["MigrationRunQueue:QueueName"]
        }))
        .WithName("DebugConfig")
        .WithTags("Diagnostics")
        .WithSummary("Returns non-secret runtime configuration values used by the Admin API.");

        return app;
    }
}


