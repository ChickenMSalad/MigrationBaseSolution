namespace MigrationBase.Core.Cloud.Azure.Workers.Runtime;

public enum AzureWorkerRuntimeLoopState
{
    NotStarted = 0,
    Starting = 1,
    Running = 2,
    Idle = 3,
    Draining = 4,
    Stopped = 5,
    Faulted = 6,
    Disabled = 7
}
