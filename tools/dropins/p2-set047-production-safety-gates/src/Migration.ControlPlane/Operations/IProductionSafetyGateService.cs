namespace Migration.ControlPlane.Operations;

public interface IProductionSafetyGateService
{
    ProductionSafetyGateSnapshot GetSnapshot();
}
