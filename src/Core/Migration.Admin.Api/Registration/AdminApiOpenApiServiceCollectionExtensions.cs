using Microsoft.OpenApi.Models;

namespace Migration.Admin.Api.Registration;

public static class AdminApiOpenApiServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationAdminApiOpenApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Migration Admin API",
                Version = "v1",
                Description = "Control-plane API for connector discovery, migration project setup, preflight requests, run queueing, and run status."
            });
        });

        return services;
    }
}


