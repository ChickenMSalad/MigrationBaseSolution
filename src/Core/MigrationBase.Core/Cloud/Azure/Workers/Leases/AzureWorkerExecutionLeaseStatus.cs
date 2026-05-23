namespace MigrationBase.Core.Cloud.Azure.Workers.Leases;

public enum AzureWorkerExecutionLeaseStatus
{
    Unknown = 0,
    Available = 1,
    Acquired = 2,
    Renewing = 3,
    Released = 4,
    Expired = 5,
    Abandoned = 6,
    Fenced = 7
}
