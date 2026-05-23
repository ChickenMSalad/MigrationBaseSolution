namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Classifies the Azure resource type used to host a MigrationBase runtime component.
/// </summary>
public enum AzureDeploymentTargetKind
{
    Unknown = 0,
    AppService = 1,
    ContainerApp = 2,
    FunctionApp = 3,
    WebJob = 4,
    WorkerService = 5,
    SqlDatabase = 6,
    StorageAccount = 7,
    ServiceBusNamespace = 8,
    KeyVault = 9,
    ApplicationInsights = 10
}
