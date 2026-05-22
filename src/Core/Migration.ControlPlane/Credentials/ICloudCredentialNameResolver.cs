namespace Migration.ControlPlane.Credentials;

public interface ICloudCredentialNameResolver
{
    CloudCredentialReference Resolve(
        string workspaceId,
        string connectorRole,
        string connectorKey,
        string credentialSetId,
        string secretKind);
}
