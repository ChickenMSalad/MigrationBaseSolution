namespace Migration.ControlPlane.Credentials;

public sealed class NullCloudCredentialValueProvider : ICloudCredentialValueProvider
{
    public Task<bool> ExistsAsync(
        CloudCredentialReference reference,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<string> GetSecretValueAsync(
        CloudCredentialReference reference,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("""
Cloud credential value provider is not configured.

Local development can continue using existing user-secrets/config paths.
Configure Key Vault mode before resolving cloud credential values.
""");
}
