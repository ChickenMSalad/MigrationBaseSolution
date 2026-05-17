using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Queues;

public static class QueueExecutorCoordinatorRegistrationExtensions
{
    public static IServiceCollection AddQueueExecutorCoordinator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(sp =>
        {
            var receiveProvider = sp.GetRequiredService<IQueueReceiveProvider>();
            var options = QueuePoisonHandlingPlanner.BuildOptions(configuration);
            return QueuePoisonHandlingPlanner.BuildPlan(options, receiveProvider.Descriptor);
        });

        services.AddSingleton<IQueueExecutorCoordinator, QueueExecutorCoordinator>();

        return services;
    }

    public static QueueExecutorCoordinatorOptions BuildOptions(IConfiguration configuration)
    {
        return new QueueExecutorCoordinatorOptions(
            DryRun: ReadBool(configuration, "QueueExecutorCoordinator:DryRun", true),
            CompleteMessages: ReadBool(configuration, "QueueExecutorCoordinator:CompleteMessages", false),
            WriteFailureArtifacts: ReadBool(configuration, "QueueExecutorCoordinator:WriteFailureArtifacts", true),
            MaxMessages: ReadInt(configuration, "QueueExecutorCoordinator:MaxMessages", 1));
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var parsed)
            ? fallback
            : Math.Max(1, parsed);
    }
}
