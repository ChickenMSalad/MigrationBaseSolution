using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Migration.Application.Abstractions;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Descriptors;
using Migration.Orchestration.Execution;
using Migration.Orchestration.Options;
using Migration.Orchestration.Progress;
using Migration.Orchestration.Retry;
using Migration.Orchestration.State;
using Migration.Orchestration.Validation;

namespace Migration.Orchestration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationOrchestration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MigrationExecutionOptions>(configuration.GetSection(MigrationExecutionOptions.SectionName));

        services.AddSingleton<IMigrationJobRunner, GenericMigrationJobRunner>();
        AddConfiguredStateStore(services, configuration);
        services.AddSingleton<StateInspectionService>();
        services.AddSingleton<IJobStateStore, LegacyJobStateStoreBridge>();
        services.AddSingleton<IMigrationRetryPolicy, SimpleMigrationRetryPolicy>();
        services.AddSingleton<IConnectorCatalog, KnownConnectorCatalog>();

        services.AddSingleton<IValidationStep, ManifestRequiredColumnValidationStep>();
        services.AddSingleton<IValidationStep, TargetBinaryValidationStep>();
        services.AddSingleton<IValidationStep, RequiredTargetFieldsValidationStep>();

        AddConfiguredProgressSinks(services, configuration);
        services.AddSingleton<IMigrationProgressReporter, CompositeMigrationProgressReporter>();

        return services;
    }

    public static IServiceCollection AddConsoleMigrationProgress(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMigrationProgressSink, ConsoleMigrationProgressSink>());
        return services;
    }

    public static IServiceCollection AddAzureQueueMigrationProgress(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMigrationProgressSink, AzureQueueMigrationProgressSink>());
        return services;
    }

    private static void AddConfiguredStateStore(IServiceCollection services, IConfiguration configuration)
    {
        var stateStore = configuration.GetSection(MigrationExecutionOptions.SectionName).GetValue<string>("StateStore")
                         ?? "JsonFile";

        if (stateStore.Equals("AzureTable", StringComparison.OrdinalIgnoreCase) ||
            stateStore.Equals("AzureTableStorage", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<AzureTableMigrationExecutionStateStore>();
            services.AddSingleton<IMigrationExecutionStateStore>(sp => sp.GetRequiredService<AzureTableMigrationExecutionStateStore>());
            services.AddSingleton<IMigrationExecutionStateMaintenance>(sp => sp.GetRequiredService<AzureTableMigrationExecutionStateStore>());
            return;
        }

        services.AddSingleton<JsonFileMigrationExecutionStateStore>();
        services.AddSingleton<IMigrationExecutionStateStore>(sp => sp.GetRequiredService<JsonFileMigrationExecutionStateStore>());
        services.AddSingleton<IMigrationExecutionStateMaintenance>(sp => sp.GetRequiredService<JsonFileMigrationExecutionStateStore>());
    }

    private static void AddConfiguredProgressSinks(IServiceCollection services, IConfiguration configuration)
    {
        // Structured logging remains the default local/cloud-safe sink.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMigrationProgressSink, LoggingMigrationProgressSink>());

        var configuredSinks = configuration
            .GetSection($"{MigrationExecutionOptions.SectionName}:ProgressSinks")
            .Get<List<string>>()
            ?? new List<string>();

        if (Contains(configuredSinks, "Console"))
        {
            services.AddConsoleMigrationProgress();
        }

        if (Contains(configuredSinks, "AzureQueue") || Contains(configuredSinks, "Queue"))
        {
            services.AddAzureQueueMigrationProgress();
        }
    }

    private static bool Contains(IEnumerable<string> values, string value)
    {
        return values.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
    }
}
