namespace MigrationBase.Core.Cloud.Azure.Workers;

public sealed record AzureWorkerDrainOptions
{
    public bool DrainOnShutdown { get; init; } = true;

    public TimeSpan DrainTimeout { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan StopAcceptingWorkTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public bool AbandonUnfinishedWorkOnTimeout { get; init; } = true;

    public static AzureWorkerDrainOptions Defaults { get; } = new();
}
