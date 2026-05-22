using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Migration.ControlPlane.Credentials;

public sealed class KeyVaultCloudCredentialValueProvider : ICloudCredentialValueProvider
{
    private readonly SecretClient _client;

    public KeyVaultCloudCredentialValueProvider(Uri keyVaultUri)
    {
        _client = new SecretClient(keyVaultUri, new DefaultAzureCredential());
    }

    public async Task<bool> ExistsAsync(
        CloudCredentialReference reference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);

        try
        {
            await _client.GetSecretAsync(reference.SecretName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task<string> GetSecretValueAsync(
        CloudCredentialReference reference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var response = await _client.GetSecretAsync(reference.SecretName, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.Value.Value;
    }
}
