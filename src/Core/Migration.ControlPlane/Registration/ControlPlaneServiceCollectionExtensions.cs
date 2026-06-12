using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.ControlPlane.Artifacts;
using Migration.ControlPlane.Options;
using Migration.ControlPlane.Progress;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Migration.Orchestration.Preflight;
using Migration.Orchestration.Progress;
using Migration.ControlPlane.ManifestBuilder;
using System.Net.Http;

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
        services.AddSingleton<IArtifactStore, FileBackedArtifactStore>();
        //services.AddScoped<IAdminProjectStore, SqlAdminProjectStore>();
        services.AddSingleton<IAdminProjectStore, SqlAdminProjectStore>();
        services.AddSingleton<ArtifactPathResolver>();
        //services.AddSingleton<IAdminProjectStore, FileBackedAdminProjectStore>();
        services.AddSingleton<IRunMonitoringStore, FileBackedRunMonitoringStore>();
        services.AddSingleton<RunMonitoringService>();
        services.AddSingleton<ControlPlaneDeleteService>();
        services.AddSingleton<MigrationPreflightService>();

        services.AddSingleton<ManifestBuilderServiceRegistry>();
        services.AddSingleton<ManifestBuilderFileStore>();

        // Manifest builders exposed through /api/manifest-builder.
        services.AddSingleton<ISourceManifestService, SharePointRcloneSourceManifestService>();
        services.AddSingleton<ISourceManifestService, AemExportFoldersSourceManifestService>();
        services.AddSingleton<ISourceManifestService, BynderExportAssetsSourceManifestService>();
        services.AddSingleton<ISourceManifestService, ContentHubTaxonomiesSourceManifestService>();

        // Credential metadata is shared through SQL when a runtime SQL connection is configured.
        // Secret values may still live in Key Vault; the SQL store persists only credential-set
        // metadata and secret references such as kv://secret-name. File-backed storage remains
        // a local/dev fallback and a one-time migration source for existing file records.
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
