namespace Migration.ControlPlane.Auth;

public interface IEndpointPolicyInventoryService
{
    EndpointPolicyInventorySnapshot GetSnapshot();
}
