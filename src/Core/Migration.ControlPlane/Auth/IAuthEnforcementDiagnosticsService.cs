namespace Migration.ControlPlane.Auth;

public interface IAuthEnforcementDiagnosticsService
{
    AuthEnforcementDiagnosticsSnapshot GetSnapshot();
}
