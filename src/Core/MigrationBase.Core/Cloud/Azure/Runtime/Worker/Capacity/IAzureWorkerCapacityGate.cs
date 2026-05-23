namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker.Capacity;

public interface IAzureWorkerCapacityGate
{
    AzureWorkerCapacityDecision Evaluate(AzureWorkerCapacitySnapshot snapshot, AzureWorkerCapacityLimit limit);
}
