namespace MigrationBase.Core.Cloud.Azure.Hosting;

/// <summary>
/// Defines the logical runtime role a deployable host performs in Azure.
/// </summary>
public enum AzureHostRoleKind
{
    Unknown = 0,
    AdminApi = 1,
    OperatorUi = 2,
    QueueExecutor = 3,
    ServiceBusDispatcher = 4,
    ServiceBusExecutor = 5,
    ManifestIngestion = 6,
    OperationalMaintenance = 7
}
