namespace Migration.ControlPlane.Operations;

public interface IOperationalModeService
{
    OperationalModeSnapshot GetSnapshot();
}
