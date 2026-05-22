namespace Migration.ControlPlane.Credentials;

public interface ICloudCredentialValueProvider
{
    Task<bool> ExistsAsync(
        CloudCredentialReference reference,
        CancellationToken cancellationToken = default);

    Task<string> GetSecretValueAsync(
        CloudCredentialReference reference,
        CancellationToken cancellationToken = default);
}
