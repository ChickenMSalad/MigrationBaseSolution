namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker.Capacity;

public enum AzureWorkerCapacityDecisionKind
{
    Accepted = 0,
    Throttled = 1,
    Rejected = 2,
    Draining = 3,
    Stopped = 4
}
