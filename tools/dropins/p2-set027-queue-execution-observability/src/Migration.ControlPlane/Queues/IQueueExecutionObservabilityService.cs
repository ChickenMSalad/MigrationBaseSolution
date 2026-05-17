namespace Migration.ControlPlane.Queues;

public interface IQueueExecutionObservabilityService
{
    QueueExecutionObservabilitySnapshot GetSnapshot();
}
