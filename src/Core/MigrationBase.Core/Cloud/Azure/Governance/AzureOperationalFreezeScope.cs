namespace MigrationBase.Core.Cloud.Azure.Governance;

public enum AzureOperationalFreezeScope
{
    Unknown = 0,
    Environment = 1,
    Tenant = 2,
    HostRole = 3,
    MigrationRun = 4,
    Queue = 5
}
