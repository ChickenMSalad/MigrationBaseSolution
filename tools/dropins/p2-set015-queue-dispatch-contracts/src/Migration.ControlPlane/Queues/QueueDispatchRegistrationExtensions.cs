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
            var storageAccountName = FirstNonEmpty(
                configuration["MigrationRunQueue:StorageAccountName"],
                configuration["AzureQueue:StorageAccountName"],
                configuration["Cloud:QueueStorageAccountName"]);

            if (string.IsNullOrWhiteSpace(storageAccountName))
            {
                services.AddSingleton<IQueueDispatchProvider>(_ => new NullQueueDispatchProvider(
                    "azureStorageQueue",
                    queueName,
                    ["Azure Queue provider is selected but no storage account name is configured."]));

                return services;
            }

            // Real Azure Queue dispatch lands in a later P2 set.
            services.AddSingleton<IQueueDispatchProvider>(_ => new NullQueueDispatchProvider(
                "azureStorageQueue",
                queueName,
                ["Azure Queue provider configuration is present, but dispatch implementation has not been enabled yet."]));

            return services;
        }

        services.AddSingleton<IQueueDispatchProvider>(_ => new NullQueueDispatchProvider(
            provider,
            queueName,
            [$"Queue provider '{provider}' is not recognized by the dispatch abstraction."]));

        return services;
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
