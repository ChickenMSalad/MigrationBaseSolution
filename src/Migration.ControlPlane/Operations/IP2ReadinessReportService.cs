namespace Migration.ControlPlane.Operations;

public interface IP2ReadinessReportService
{
    P2ReadinessReportSnapshot GetSnapshot();
}
