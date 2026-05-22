namespace Migration.ControlPlane.Operations;

public interface IOperationalReadinessService
{
    OperationalReadinessSnapshot GetSnapshot();
}
