using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.ControlPlane.Registration;
using Migration.GenericRuntime.Registration;
using Migration.Infrastructure.Taxonomy;

namespace Migration.Admin.Api.Registration;

/// <summary>
/// Composition root for Migration.Admin.Api.
///
/// Keep Program.cs focused on HTTP pipeline and endpoint mapping. Runtime,
/// control-plane, manifest-builder, connector, taxonomy, and orchestration
/// services are registered here.
/// </summary>
public static class AdminApiServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationAdminApiRuntime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Shared execution/runtime path used by API and worker hosts.
        services.AddMigrationRuntime(configuration);

        // Control-plane storage, queues, project/run stores, credentials,
        // artifact helpers, progress monitoring, and manifest builders.
        services.AddMigrationControlPlane(configuration);

        // Admin-only taxonomy builder endpoint support.
        services.AddTaxonomyBuilder(configuration);

        return services;
    }
}


