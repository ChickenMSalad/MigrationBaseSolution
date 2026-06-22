using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.ControlPlane.Registration;
using Migration.GenericRuntime.Registration;
using Migration.Workers.QueueExecutor.Services;

namespace Migration.Workers.QueueExecutor.Registration;

public static class SqlOperationalMigrationJobRuntimeRegistrationExtensions
{
    public static IServiceCollection AddSqlOperationalMigrationJobRuntime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var runtimeConfiguration = BuildOperationalRuntimeConfiguration(configuration);

        services.AddMigrationControlPlane(runtimeConfiguration);

        // Generic runtime is the single composition point for manifest providers,
        // source connectors, target connectors, mapping, validation, and orchestration.
        // Do not call AddMigrationConnectorModules here; that bypasses GenericMigrationRuntime
        // filters and can register duplicate or partially composed connectors.
        services.AddGenericMigrationRuntime(runtimeConfiguration);

        services.AddSingleton<ProjectCredentialJobSettingsHydrator>();

        return services;
    }

    private static IConfiguration BuildOperationalRuntimeConfiguration(IConfiguration configuration)
    {
        var defaults = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["GenericMigrationRuntime:RegisterAllWhenEmpty"] = "false",
            ["GenericMigrationRuntime:EnabledSources:0"] = "AzureBlob",
            ["GenericMigrationRuntime:EnabledSources:1"] = "WebDam",
            ["GenericMigrationRuntime:EnabledSources:2"] = "LocalStorage",
            ["GenericMigrationRuntime:EnabledSources:3"] = "S3",
            ["GenericMigrationRuntime:EnabledSources:4"] = "SharePoint",
            ["GenericMigrationRuntime:EnabledSources:5"] = "Bynder",
            ["GenericMigrationRuntime:EnabledTargets:0"] = "Bynder",
            ["GenericMigrationRuntime:EnabledTargets:1"] = "AzureBlob",
            ["GenericMigrationRuntime:EnabledTargets:2"] = "LocalStorage",
            ["GenericMigrationRuntime:EnabledTargets:3"] = "Aprimo",
            ["GenericMigrationRuntime:EnabledTargets:4"] = "Cloudinary",
            ["GenericMigrationRuntime:EnabledManifests:0"] = "Csv",
            ["GenericMigrationRuntime:EnabledManifests:1"] = "Excel",
            ["GenericMigrationRuntime:EnabledManifests:2"] = "Sql",
            ["GenericMigrationRuntime:EnabledManifests:3"] = "Sqlite"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(defaults)
            .AddConfiguration(configuration)
            .Build();
    }
}
