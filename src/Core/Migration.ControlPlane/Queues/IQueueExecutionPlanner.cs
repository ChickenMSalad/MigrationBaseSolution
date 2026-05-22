namespace Migration.ControlPlane.Queues;

public interface IQueueExecutionPlanner
{
    QueueExecutionPlan Plan(QueueMessageEnvelope envelope);
}
