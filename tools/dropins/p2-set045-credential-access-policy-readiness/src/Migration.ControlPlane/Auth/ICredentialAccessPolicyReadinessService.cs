namespace Migration.ControlPlane.Auth;

public interface ICredentialAccessPolicyReadinessService
{
    CredentialAccessPolicyReadinessSnapshot GetSnapshot();
}
