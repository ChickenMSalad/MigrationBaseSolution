namespace MigrationBase.Core.Cloud.Azure.Workers;

public enum AzureWorkerLifecyclePhase
{
    Unknown = 0,
    Starting = 10,
    Ready = 20,
    Draining = 30,
    Stopping = 40,
    Stopped = 50,
    Faulted = 60
}
