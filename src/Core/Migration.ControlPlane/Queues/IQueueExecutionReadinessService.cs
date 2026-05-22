namespace Migration.ControlPlane.Queues;

public interface IQueueExecutionReadinessService
{
    QueueExecutionReadinessSnapshot GetSnapshot();
}
