using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;

namespace Migration.Workers.ServiceBusExecutor.Smoke;

public static class RuntimeSmokeProviderNames
{
    public const string Type = "RuntimeSmoke";
}

public static class RuntimeSmokeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the no-op smoke provider set for the Service Bus executor host.
    /// This should stay local to runtime smoke execution and must not replace
    /// production manifest/source/target provider registrations.
    /// </summary>
    public static IServiceCollection AddRuntimeSmokeExecutionProviders(this IServiceCollection services)
    {
        services.AddSingleton<IManifestProvider, RuntimeSmokeManifestProvider>();
        services.AddSingleton<IAssetSourceConnector, RuntimeSmokeSourceConnector>();
        services.AddSingleton<IAssetTargetConnector, RuntimeSmokeTargetConnector>();

        return services;
    }
}
