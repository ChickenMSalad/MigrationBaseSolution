using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Migration.ControlPlane.Services;

public sealed class FileBackedCredentialResolver : ICredentialResolver
{
    
    private readonly ICredentialSetStore _store;
    private readonly IConfiguration _configuration;

    public FileBackedCredentialResolver(
        ICredentialSetStore store,
        IConfiguration configuration)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        string credentialSetId,
        CancellationToken cancellationToken = default)
    {
        var credentialSet = await _store.GetAsync(credentialSetId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Credential set '{credentialSetId}' was not found.");

        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in credentialSet.Values)
        {
            if (credentialSet.SecretKeys.Contains(pair.Key))
            {
                resolved[pair.Key] = await ResolveSecretReferenceAsync(pair.Key, pair.Value, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                resolved[pair.Key] = pair.Value;
            }
        }

        return resolved;
    }

    private async Task<string> ResolveSecretReferenceAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Credential value '{key}' is empty.");
        }

        if (TryReadEnvironmentReference(value, out var environmentName))
        {
            var environmentValue = Environment.GetEnvironmentVariable(environmentName);
            if (string.IsNullOrWhiteSpace(environmentValue))
            {
                throw new InvalidOperationException(
                    $"Credential value '{key}' references environment variable '{environmentName}', but it is not configured.");
            }

            return environmentValue;
        }

        if (TryReadKeyVaultReference(value, out var vaultUri, out var secretName, out var secretVersion))
        {
            return await ReadKeyVaultSecretAsync(vaultUri, secretName, secretVersion, cancellationToken)
                .ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Credential value '{key}' must be a secret reference. Use kv://secret-name, env://VARIABLE_NAME, or a Key Vault secret URI.");
    }

    private bool TryReadEnvironmentReference(string value, out string name)
    {
        const string prefix = "env://";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            name = value[prefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(name);
        }

        name = string.Empty;
        return false;
    }

    private bool TryReadKeyVaultReference(
        string value,
        out Uri vaultUri,
        out string secretName,
        out string? secretVersion)
    {
        secretVersion = null;

        if (value.StartsWith("kv://", StringComparison.OrdinalIgnoreCase))
        {
            var configuredVaultUri = GetConfiguredKeyVaultUri();
            var raw = value["kv://".Length..].Trim('/');
            var parts = raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            secretName = parts.Length > 0 ? parts[0] : string.Empty;
            secretVersion = parts.Length > 1 ? parts[1] : null;
            vaultUri = configuredVaultUri;
            return !string.IsNullOrWhiteSpace(secretName);
        }

        if (value.StartsWith("keyvault://", StringComparison.OrdinalIgnoreCase))
        {
            var configuredVaultUri = GetConfiguredKeyVaultUri();
            var raw = value["keyvault://".Length..].Trim('/');
            var parts = raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            secretName = parts.Length > 0 ? parts[0] : string.Empty;
            secretVersion = parts.Length > 1 ? parts[1] : null;
            vaultUri = configuredVaultUri;
            return !string.IsNullOrWhiteSpace(secretName);
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase))
        {
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2 && string.Equals(parts[0], "secrets", StringComparison.OrdinalIgnoreCase))
            {
                vaultUri = new Uri($"{uri.Scheme}://{uri.Host}");
                secretName = parts[1];
                secretVersion = parts.Length > 2 ? parts[2] : null;
                return true;
            }
        }

        vaultUri = new Uri("https://localhost");
        secretName = string.Empty;
        return false;
    }

    private Uri GetConfiguredKeyVaultUri()
    {
        var value = _configuration["CredentialVault:KeyVaultUri"]
            ?? _configuration["AzureKeyVault:VaultUri"]
            ?? _configuration["KeyVault:VaultUri"];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Key Vault URI is not configured. Configure CredentialVault:KeyVaultUri or use full Key Vault secret URIs.");
        }

        return Uri.TryCreate(value.TrimEnd('/'), UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidOperationException($"Configured Key Vault URI '{value}' is not a valid absolute URI.");
    }

    private static async Task<string> ReadKeyVaultSecretAsync(
        Uri vaultUri,
        string secretName,
        string? secretVersion,
        CancellationToken cancellationToken)
    {
        var client = new SecretClient(
            vaultUri,
            new DefaultAzureCredential());

        var response = string.IsNullOrWhiteSpace(secretVersion)
            ? await client.GetSecretAsync(
                secretName,
                cancellationToken: cancellationToken)
            : await client.GetSecretAsync(
                secretName,
                secretVersion,
                cancellationToken);

        return response.Value.Value;
    }

}
