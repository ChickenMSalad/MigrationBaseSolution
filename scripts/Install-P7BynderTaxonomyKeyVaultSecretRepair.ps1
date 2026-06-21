[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-File {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Required file not found: ' + $Path)
    }
}

function Backup-File {
    param([string]$Path)
    $stamp = Get-Date -Format 'yyyyMMddHHmmss'
    $backup = $Path + '.p7-bynder-taxonomy-secret.' + $stamp + '.bak'
    Copy-Item -LiteralPath $Path -Destination $backup -Force
    Write-Host ('Backed up ' + $Path)
}

$repo = (Resolve-Path -LiteralPath $RepoRoot).Path
$target = Join-Path $repo 'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs'
Assert-File -Path $target
Backup-File -Path $target

$content = Get-Content -LiteralPath $target -Raw

if ($content.IndexOf('ResolveCredentialValueAsync(', [System.StringComparison]::Ordinal) -ge 0) {
    Write-Host 'Bynder taxonomy credential secret resolution already present.'
    return
}

if ($content.IndexOf('using Azure.Identity;', [System.StringComparison]::Ordinal) -lt 0) {
    $content = $content.Replace('using System.Text.Json.Nodes;' + [Environment]::NewLine, 'using System.Text.Json.Nodes;' + [Environment]::NewLine + 'using Azure.Identity;' + [Environment]::NewLine + 'using Azure.Security.KeyVault.Secrets;' + [Environment]::NewLine)
}

$content = $content.Replace(
    '            ILoggerFactory loggerFactory,' + [Environment]::NewLine + '            CancellationToken cancellationToken) =>',
    '            ILoggerFactory loggerFactory,' + [Environment]::NewLine + '            IConfiguration configuration,' + [Environment]::NewLine + '            CancellationToken cancellationToken) =>')

$content = $content.Replace(
    '"bynder" => await ReadBynderTaxonomyAsync(credential, request, cancellationToken).ConfigureAwait(false),',
    '"bynder" => await ReadBynderTaxonomyAsync(credential, request, configuration, cancellationToken).ConfigureAwait(false),')

$startMarker = '    private static async Task<TaxonomySnapshot> ReadBynderTaxonomyAsync('
$endMarker = '    private static IEnumerable<JsonObject> BynderMetapropertyObjects('
$start = $content.IndexOf($startMarker, [System.StringComparison]::Ordinal)
$end = $content.IndexOf($endMarker, [System.StringComparison]::Ordinal)
if ($start -lt 0 -or $end -lt 0 -or $end -le $start) {
    throw 'Could not locate ReadBynderTaxonomyAsync method boundaries. Refusing to guess.'
}

$newMethod = @'
    private static async Task<TaxonomySnapshot> ReadBynderTaxonomyAsync(
        CredentialSetRecord credential,
        BuildTaxonomyArtifactRequest request,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var baseUrl = await ResolveCredentialValueAsync(
            credential.Values,
            configuration,
            cancellationToken,
            "BaseUrl",
            "baseUrl",
            "BynderBaseUrl",
            "bynderBaseUrl",
            "TargetCredential_BaseUrl",
            "targetCredential_BaseUrl",
            "url",
            "Url",
            "domain",
            "Domain",
            "bynderDomain",
            "BynderDomain").ConfigureAwait(false);

        var clientId = await ResolveCredentialValueAsync(
            credential.Values,
            configuration,
            cancellationToken,
            "ClientId",
            "clientId",
            "BynderClientId",
            "bynderClientId",
            "TargetCredential_ClientId",
            "targetCredential_ClientId").ConfigureAwait(false);

        var clientSecret = await ResolveCredentialValueAsync(
            credential.Values,
            configuration,
            cancellationToken,
            "ClientSecret",
            "clientSecret",
            "BynderClientSecret",
            "bynderClientSecret",
            "TargetCredential_ClientSecret",
            "targetCredential_ClientSecret").ConfigureAwait(false);

        var scopes = await ResolveCredentialValueAsync(
            credential.Values,
            configuration,
            cancellationToken,
            "Scopes",
            "scopes",
            "BynderScopes",
            "bynderScopes",
            "TargetCredential_Scopes",
            "targetCredential_Scopes").ConfigureAwait(false);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(baseUrl)) missing.Add("BaseUrl");
        if (string.IsNullOrWhiteSpace(clientId)) missing.Add("ClientId");
        if (string.IsNullOrWhiteSpace(clientSecret)) missing.Add("ClientSecret");
        if (string.IsNullOrWhiteSpace(scopes)) missing.Add("Scopes");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Bynder taxonomy generation requires OAuth credential value(s): " +
                string.Join(", ", missing) +
                ". Expected BaseUrl, ClientId, ClientSecret, and Scopes from the selected Bynder target credential set.");
        }

        var bynderClient = new BynderRestClient(baseUrl!, clientId!, clientSecret!, scopes!);
        var apiClient = await bynderClient.GetAuthenticatedClientAsync().ConfigureAwait(false);
        var response = await apiClient.ExecuteAsync(new RestRequest("api/v4/metaproperties/", Method.Get), cancellationToken).ConfigureAwait(false);
        var json = response.Content ?? string.Empty;

        if (!response.IsSuccessful || string.IsNullOrWhiteSpace(json))
        {
            throw new HttpRequestException($"Bynder metaproperties request failed: {(int)response.StatusCode} {response.StatusDescription}.{Environment.NewLine}{json}");
        }

        var root = JsonNode.Parse(json) ?? new JsonArray();
        var fields = new List<TaxonomyField>();
        var options = new List<TaxonomyOption>();

        foreach (var item in BynderMetapropertyObjects(root))
        {
            var id = Text(item, "id", "uuid", "name", "propertyId");
            var fieldName = Text(item, "name", "propertyName", "technicalName", "id");
            fields.Add(new TaxonomyField(
                id,
                fieldName,
                Text(item, "label", "displayName", "name", "propertyName"),
                Text(item, "type", "inputType", "fieldType"),
                Bool(item, "required", "isRequired", "mandatory"),
                Bool(item, "isMultiselect", "isMultiSelect", "multiSelect", "multiselect", "multiple"),
                Text(item, "description", "helpText"),
                item.ToJsonString(JsonOptions)));

            foreach (var option in BynderMetapropertyOptions(item, id, fieldName))
            {
                options.Add(option);
            }
        }

        return new TaxonomySnapshot("Bynder", fields, options, json);
    }

    private static async Task<string?> ResolveCredentialValueAsync(
        IReadOnlyDictionary<string, string?> values,
        IConfiguration configuration,
        CancellationToken cancellationToken,
        params string[] keys)
    {
        var value = FirstValue(values, keys);
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        value = value.Trim();

        if (value.StartsWith("env://", StringComparison.OrdinalIgnoreCase))
        {
            var variableName = value.Substring("env://".Length).Trim();
            return string.IsNullOrWhiteSpace(variableName)
                ? value
                : Environment.GetEnvironmentVariable(variableName) ?? value;
        }

        if (value.StartsWith("kv://", StringComparison.OrdinalIgnoreCase))
        {
            var secretName = value.Substring("kv://".Length).Trim('/');
            return await ReadKeyVaultSecretByNameAsync(configuration, secretName, cancellationToken).ConfigureAwait(false);
        }

        if (value.StartsWith("keyvault://", StringComparison.OrdinalIgnoreCase))
        {
            var secretName = value.Substring("keyvault://".Length).Trim('/');
            return await ReadKeyVaultSecretByNameAsync(configuration, secretName, cancellationToken).ConfigureAwait(false);
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var secretUri)
            && secretUri.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase)
            && secretUri.AbsolutePath.Contains("/secrets/", StringComparison.OrdinalIgnoreCase))
        {
            return await ReadKeyVaultSecretByUriAsync(secretUri, cancellationToken).ConfigureAwait(false);
        }

        return value;
    }

    private static async Task<string> ReadKeyVaultSecretByNameAsync(
        IConfiguration configuration,
        string secretName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new InvalidOperationException("Credential secret reference did not include a Key Vault secret name.");
        }

        var vaultUri = GetConfiguredKeyVaultUri(configuration);
        if (vaultUri is null)
        {
            throw new InvalidOperationException(
                "Credential value is a Key Vault reference, but no Key Vault URI is configured. Expected CredentialVault:KeyVaultUri, AzureKeyVault:VaultUri, or KeyVault:VaultUri.");
        }

        var client = new SecretClient(vaultUri, new DefaultAzureCredential());
        var response = await client.GetSecretAsync(secretName, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.Value;
    }

    private static async Task<string> ReadKeyVaultSecretByUriAsync(Uri secretUri, CancellationToken cancellationToken)
    {
        var pathParts = secretUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var secretIndex = Array.FindIndex(pathParts, x => string.Equals(x, "secrets", StringComparison.OrdinalIgnoreCase));
        if (secretIndex < 0 || secretIndex + 1 >= pathParts.Length)
        {
            throw new InvalidOperationException($"Key Vault secret URI '{secretUri}' does not contain a secret name.");
        }

        var vaultBase = new Uri($"{secretUri.Scheme}://{secretUri.Host}/");
        var secretName = pathParts[secretIndex + 1];
        var version = secretIndex + 2 < pathParts.Length ? pathParts[secretIndex + 2] : null;
        var client = new SecretClient(vaultBase, new DefaultAzureCredential());
        var response = string.IsNullOrWhiteSpace(version)
            ? await client.GetSecretAsync(secretName, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await client.GetSecretAsync(secretName, version, cancellationToken).ConfigureAwait(false);
        return response.Value.Value;
    }

    private static Uri? GetConfiguredKeyVaultUri(IConfiguration configuration)
    {
        var value = configuration["CredentialVault:KeyVaultUri"]
            ?? configuration["AzureKeyVault:VaultUri"]
            ?? configuration["KeyVault:VaultUri"];

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value.TrimEnd('/'), UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidOperationException($"Configured Key Vault URI '{value}' is not a valid absolute URI.");
    }

'@

$content = $content.Substring(0, $start) + $newMethod + $content.Substring($end)

Set-Content -LiteralPath $target -Value $content -Encoding UTF8
Write-Host 'Installed Bynder taxonomy Key Vault secret resolution repair.'
