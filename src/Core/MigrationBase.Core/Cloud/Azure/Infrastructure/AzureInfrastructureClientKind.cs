namespace MigrationBase.Core.Cloud.Azure.Infrastructure;

public enum AzureInfrastructureClientKind
{
    Unknown = 0,
    Sql = 1,
    BlobStorage = 2,
    Queue = 3,
    KeyVault = 4,
    ApplicationInsights = 5,
    ServiceBus = 6
}
