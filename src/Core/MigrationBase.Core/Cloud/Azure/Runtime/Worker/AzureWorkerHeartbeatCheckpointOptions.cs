namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker;

public sealed class AzureWorkerHeartbeatCheckpointOptions
{
    public const string SectionName = "AzureRuntime:Worker:HeartbeatCheckpoint";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan StaleAfter { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan FaultedAfter { get; set; } = TimeSpan.FromMinutes(5);

    public int MaxProperties { get; set; } = 32;

    public bool RequireExecutionRunIdWhenRunningWork { get; set; } = true;
}
