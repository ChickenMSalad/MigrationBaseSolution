namespace MigrationBase.Core.Cloud.Azure.Workers.Heartbeat;

public sealed class AzureWorkerHeartbeatOptions
{
    public const string SectionName = "AzureRuntime:Workers:Heartbeat";

    public bool Enabled { get; set; } = true;

    public int IntervalSeconds { get; set; } = 30;

    public int StaleAfterSeconds { get; set; } = 180;

    public int LostAfterSeconds { get; set; } = 600;

    public bool RequireHostRole { get; set; } = true;

    public bool IncludeCapacitySnapshot { get; set; } = true;

    public bool IncludeDrainState { get; set; } = true;

    public bool IncludeExecutionIsolationState { get; set; } = true;

    public int MinimumIntervalSeconds => 5;

    public bool IsTimingValid() =>
        IntervalSeconds >= MinimumIntervalSeconds &&
        StaleAfterSeconds > IntervalSeconds &&
        LostAfterSeconds > StaleAfterSeconds;
}
