namespace Migration.ControlPlane.Queues;

public interface IQueueExecutorCoordinator
{
    Task<QueueExecutorCoordinatorResult> PollOnceAsync(
        QueueExecutorCoordinatorOptions options,
        CancellationToken cancellationToken = default);
}
