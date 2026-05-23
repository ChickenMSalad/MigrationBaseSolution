namespace MigrationBase.Core.Cloud.Azure.Workers.Lifecycle;

/// <summary>
/// Describes how an Azure-hosted worker should stop accepting and completing work during shutdown.
/// </summary>
public enum AzureWorkerDrainMode
{
    None = 0,
    StopAcceptingNewWork = 1,
    CompleteActiveWork = 2,
    AbandonActiveWork = 3,
    EmergencyStop = 4
}
