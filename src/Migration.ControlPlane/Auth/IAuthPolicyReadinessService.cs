namespace Migration.ControlPlane.Auth;

public interface IAuthPolicyReadinessService
{
    AuthPolicyReadinessSnapshot GetSnapshot();
}
