using Microsoft.Extensions.Configuration;

namespace Migration.ControlPlane.Queues;

public static class QueueWorkerLoopPlanner
{
    public static QueueWorkerLoopOptions BuildOptions(IConfiguration configuration)
    {
        return new QueueWorkerLoopOptions
        {
            Enabled = ReadBool(configuration, "QueueWorkerLoop:Enabled", false),
            MaxMessages = ReadInt(configuration, "QueueWorkerLoop:MaxMessages", 1),
            PollIntervalSeconds = ReadInt(configuration, "QueueWorkerLoop:PollIntervalSeconds", 10),
            VisibilityTimeoutSeconds = ReadInt(configuration, "QueueWorkerLoop:VisibilityTimeoutSeconds", 300),
            CompleteMessages = ReadBool(configuration, "QueueWorkerLoop:CompleteMessages", false),
            DryRun = ReadBool(configuration, "QueueWorkerLoop:DryRun", true)
        };
    }

    public static QueueWorkerLoopDescriptor BuildDescriptor(
        QueueWorkerLoopOptions options,
        QueueReceiveProviderDescriptor receiveProvider)
    {
        var warnings = new List<string>();

        if (!options.Enabled)
        {
            warnings.Add("Queue worker loop is disabled.");
        }

        if (!receiveProvider.IsConfigured)
        {
            warnings.Add("Queue receive provider is not configured.");
        }

        if (options.DryRun)
        {
            warnings.Add("Queue worker loop is in dry-run mode and will not execute migration runs.");
        }

        if (!options.CompleteMessages)
        {
            warnings.Add("Queue worker loop will not complete/delete received messages.");
        }

        return new QueueWorkerLoopDescriptor(
            Enabled: options.Enabled,
            DryRun: options.DryRun,
            MaxMessages: options.MaxMessages,
            PollIntervalSeconds: options.PollIntervalSeconds,
            VisibilityTimeoutSeconds: options.VisibilityTimeoutSeconds,
            ReceiveProviderKind: receiveProvider.ProviderKind,
            LogicalQueueName: receiveProvider.LogicalQueueName,
            ReceiveProviderConfigured: receiveProvider.IsConfigured,
            Warnings: warnings);
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
