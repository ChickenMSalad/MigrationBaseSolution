using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.GenericRuntime.Registration;

/// <summary>
/// Shared execution-runtime composition root for host projects.
///
/// Host projects should call this instead of individually remembering the
/// generic runtime/orchestration registration details. Host-specific concerns
/// such as HTTP endpoints, queue workers, control-plane stores, Swagger, and
/// hosted services still belong in the host project.
/// </summary>
public static class MigrationRuntimeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared generic migration execution runtime.
    ///
    /// This intentionally delegates to AddGenericMigrationRuntime because that
    /// method already owns connector, manifest-provider, mapping, validation,
    /// preflight, and orchestration wiring.
    /// </summary>
    public static IServiceCollection AddMigrationRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeConsoleProgress = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddGenericMigrationRuntime(configuration, includeConsoleProgress);

        return services;
    }
}
