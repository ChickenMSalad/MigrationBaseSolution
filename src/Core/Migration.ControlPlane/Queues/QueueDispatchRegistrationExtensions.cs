using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Queues;

public static class QueueDispatchRegistrationExtensions
{
    public static IServiceCollection AddQueueDispatchProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = FirstNonEmpty(
            configuration["MigrationRunQueue:Provider"],
            configuration["Cloud:QueueProvider"],
            "InMemory");

        var queueName = FirstNonEmpty(
            configuration["MigrationRunQueue:QueueName"],
            configuration["Cloud:QueueName"],
            "migration-runs");

        if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IQueueDispatchProvider>(_ => new InMemoryQueueDispatchProvider(queueName));
            return services;
        }

        if (provider.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase))
        {
            var options = BuildAzureQueueOptions(configuration, queueName);
            services.AddSingleton(options);

            if (options.IsConfigured)
            {
                services.AddSingleton<IQueueDispatchProvider, AzureQueueDispatchProvider>();
                return services;
            }

            services.AddSingleton<IQueueDispatchProvider>(_ => new NullQueueDispatchProvider(
                "azureStorageQueue",
                queueName,
                ["Azure Queue provider is selected but no storage account name, service URI, or connection string is configured."]));

            return services;
        }

        services.AddSingleton<IQueueDispatchProvider>(_ => new NullQueueDispatchProvider(
            provider,
            queueName,
            [$"Queue provider '{provider}' is not recognized by the dispatch abstraction."]));

        return services;
    }

    private static AzureQueueDispatchOptions BuildAzureQueueOptions(
        IConfiguration configuration,
        string queueName)
    {
        return new AzureQueueDispatchOptions
        {
            AccountName = FirstNonEmpty(
                configuration["MigrationRunQueue:StorageAccountName"],
                configuration["AzureQueue:StorageAccountName"],
                configuration["Cloud:QueueStorageAccountName"]),
            ServiceUri = FirstNonEmpty(
                configuration["AzureQueue:ServiceUri"],
                configuration["MigrationRunQueue:ServiceUri"]),
            ConnectionString = FirstNonEmpty(
                configuration["AzureQueue:ConnectionString"],
                configuration["MigrationRunQueue:ConnectionString"]),
            QueueName = queueName,
            UseManagedIdentity = ReadBool(configuration, "AzureQueue:UseManagedIdentity", true)
        };
    }

    private static bool ReadBool(
        IConfiguration configuration,
        string key,
        bool fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
