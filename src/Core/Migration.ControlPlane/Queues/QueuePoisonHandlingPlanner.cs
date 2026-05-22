using Microsoft.Extensions.Configuration;

namespace Migration.ControlPlane.Queues;

public static class QueuePoisonHandlingPlanner
{
    public static QueuePoisonHandlingOptions BuildOptions(IConfiguration configuration)
    {
        return new QueuePoisonHandlingOptions(
            MaxAttempts: ReadInt(configuration, "QueuePoisonHandling:MaxAttempts", 5),
            PoisonStrategy: FirstNonEmpty(configuration["QueuePoisonHandling:Strategy"], QueuePoisonStrategies.FailureArtifact),
            DeadLetterQueueName: FirstNonEmpty(configuration["QueuePoisonHandling:DeadLetterQueueName"], null),
            PersistFailureArtifact: ReadBool(configuration, "QueuePoisonHandling:PersistFailureArtifact", true),
            FailureArtifactKind: FirstNonEmpty(configuration["QueuePoisonHandling:FailureArtifactKind"], "queue-failures")!);
    }

    public static QueuePoisonHandlingPlan BuildPlan(
        QueuePoisonHandlingOptions options,
        QueueReceiveProviderDescriptor receiveProvider)
    {
        var warnings = new List<string>();
        var nativeDeadLetterSupported = receiveProvider.ProviderKind.Equals("serviceBus", StringComparison.OrdinalIgnoreCase);

        if (options.MaxAttempts < 1)
        {
            warnings.Add("MaxAttempts should be at least 1.");
        }

        if (receiveProvider.ProviderKind.Equals("azureStorageQueue", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Azure Storage Queue does not support native dead lettering. Use poison queue or failure artifact strategy.");
        }

        if (options.PoisonStrategy.Equals(QueuePoisonStrategies.DeadLetterQueue, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(options.DeadLetterQueueName))
        {
            warnings.Add("Dead-letter queue strategy is selected but no dead-letter queue name is configured.");
        }

        if (!receiveProvider.IsConfigured)
        {
            warnings.Add("Receive provider is not configured; poison handling is planning-only.");
        }

        return new QueuePoisonHandlingPlan(
            ProviderKind: receiveProvider.ProviderKind,
            LogicalQueueName: receiveProvider.LogicalQueueName,
            MaxAttempts: options.MaxAttempts,
            PoisonStrategy: options.PoisonStrategy,
            DeadLetterQueueName: options.DeadLetterQueueName,
            NativeDeadLetterSupported: nativeDeadLetterSupported,
            PersistFailureArtifact: options.PersistFailureArtifact,
            FailureArtifactKind: options.FailureArtifactKind,
            Warnings: warnings);
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
