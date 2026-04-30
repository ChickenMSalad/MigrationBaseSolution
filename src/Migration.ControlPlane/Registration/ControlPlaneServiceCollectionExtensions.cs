using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.ControlPlane.Artifacts;
using Migration.ControlPlane.Options;
using Migration.ControlPlane.Progress;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Services;
using Migration.Orchestration.Preflight;
using Migration.Orchestration.Progress;
using Migration.ControlPlane.ManifestBuilder;

namespace Migration.ControlPlane.Registration;

public static class ControlPlaneServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationControlPlane(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminApiOptions>(configuration.GetSection(AdminApiOptions.SectionName));
        services.Configure<MigrationRunQueueOptions>(configuration.GetSection(MigrationRunQueueOptions.SectionName));

        services.AddSingleton<AdminRunFactory>();
        services.AddSingleton<IArtifactStore, FileBackedArtifactStore>();
        services.AddSingleton<ArtifactPathResolver>();
        services.AddSingleton<IAdminProjectStore, FileBackedAdminProjectStore>();
        services.AddSingleton<IRunMonitoringStore, FileBackedRunMonitoringStore>();
        services.AddSingleton<RunMonitoringService>();

        services.AddSingleton<ControlPlaneDeleteService>();
        services.AddSingleton<MigrationPreflightService>();

        services.AddSingleton<ManifestBuilderServiceRegistry>();
        services.AddSingleton<ManifestBuilderFileStore>();

        // Credential management is additive. Legacy hosts continue to use existing appsettings/user-secrets binding.
        services.AddSingleton<CredentialSetFactory>();
        services.AddSingleton<ICredentialSetStore, FileBackedCredentialSetStore>();
        services.AddSingleton<ICredentialResolver, FileBackedCredentialResolver>();
        services.AddSingleton<CredentialTestService>();

        // Additive Step 7/monitoring sink. This does not replace console/logging/queue sinks.
        services.AddSingleton<IMigrationProgressSink, ControlPlaneMigrationProgressSink>();

        var provider = configuration.GetSection(MigrationRunQueueOptions.SectionName).GetValue<string>("Provider") ?? "None";
        if (provider.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IMigrationRunQueue, AzureQueueMigrationRunQueue>();
        }
        else
        {
            services.AddSingleton<IMigrationRunQueue, NullMigrationRunQueue>();
        }

        return services;
    }
}
