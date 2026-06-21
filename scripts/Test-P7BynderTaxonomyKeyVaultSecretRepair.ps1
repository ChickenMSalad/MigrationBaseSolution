[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath $RepoRoot).Path
$target = Join-Path $repo 'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs'
if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
    throw ('Required file not found: ' + $target)
}

$content = Get-Content -LiteralPath $target -Raw

$required = @(
    'using Azure.Identity;',
    'using Azure.Security.KeyVault.Secrets;',
    'IConfiguration configuration,',
    'ReadBynderTaxonomyAsync(credential, request, configuration, cancellationToken)',
    'ResolveCredentialValueAsync(',
    'ReadKeyVaultSecretByNameAsync(',
    'ReadKeyVaultSecretByUriAsync(',
    'new BynderRestClient(baseUrl!, clientId!, clientSecret!, scopes!)'
)

foreach ($marker in $required) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing required marker: ' + $marker)
    }
}

$forbidden = @(
    'new BynderRestClient(baseUrl, clientId, clientSecret, scopes)',
    'accessToken/apiToken'
)

foreach ($marker in $forbidden) {
    if ($content.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Forbidden stale marker found: ' + $marker)
    }
}

Write-Host 'Bynder taxonomy Key Vault secret repair validation passed.'
