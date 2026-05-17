namespace Migration.ControlPlane.Operations;

public interface IQueueExecutionGovernanceService
{
    QueueExecutionGovernanceDecision GetDecision();
}
