using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;
using Migration.ControlPlane.Artifacts;
using Migration.ControlPlane.ManifestBuilder;
using Migration.ControlPlane.Options;
using Migration.ControlPlane.Progress;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Services;
using Migration.Orchestration.Preflight;
using Migration.Orchestration.Progress;

namespace Migration.ControlPlane.Registration;

public static class ControlPlaneServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationControlPlane(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminApiOptions>(configuration.GetSection(AdminApiOptions.SectionName));
        services.Configure<MigrationRunQueueOptions>(configuration.GetSection(MigrationRunQueueOptions.SectionName));

        services.AddHttpClient();

        services.AddSingleton<AdminRunFactory>();
        services.AddSingleton<RunPreflightGateService>();

        services.AddSingleton<SqlArtifactStore>();
        services.AddSingleton<IArtifactStore>(sp => sp.GetRequiredService<SqlArtifactStore>());
        services.AddSingleton<IArtifactContentResolver>(sp => sp.GetRequiredService<SqlArtifactStore>());

        services.AddSingleton<IAdminProjectStore, SqlAdminProjectStore>();
        services.AddSingleton<ArtifactPathResolver>();
        services.AddSingleton<IRunMonitoringStore, FileBackedRunMonitoringStore>();
        services.AddSingleton<RunMonitoringService>();
        services.AddSingleton<ControlPlaneDeleteService>();
        services.AddSingleton<MigrationPreflightService>();
        services.AddSingleton<ManifestBuilderServiceRegistry>();
        services.AddSingleton<ManifestBuilderFileStore>();
services.AddSingleton<ISourceManifestService, AemExportFoldersSourceManifestService>();
        services.AddSingleton<ISourceManifestService, BynderExportAssetsSourceManifestService>();
        services.AddSingleton<ISourceManifestService, ContentHubTaxonomiesSourceManifestService>();

        services.AddSingleton<CredentialSetFactory>();
        services.AddSingleton<FileBackedCredentialSetStore>();
        services.AddSingleton<SqlCredentialSetStore>();
        services.AddSingleton<ICredentialSetStore>(sp =>
        {
            var sqlStore = sp.GetRequiredService<SqlCredentialSetStore>();
            return sqlStore.IsConfigured
                ? sqlStore
                : sp.GetRequiredService<FileBackedCredentialSetStore>();
        });
        services.AddSingleton<ICredentialResolver, FileBackedCredentialResolver>();
        services.AddSingleton<CredentialTestService>();

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
