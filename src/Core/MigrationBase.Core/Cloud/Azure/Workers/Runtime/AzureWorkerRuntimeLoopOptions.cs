namespace MigrationBase.Core.Cloud.Azure.Workers.Runtime;

public sealed class AzureWorkerRuntimeLoopOptions
{
    public string WorkerName { get; set; } = "migration-worker";

    public string EnvironmentName { get; set; } = "local";

    public int MaxIterationsPerRun { get; set; } = 1;

    public TimeSpan IdleDelay { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan ShutdownDrainTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool IsEnabled { get; set; } = true;
}
