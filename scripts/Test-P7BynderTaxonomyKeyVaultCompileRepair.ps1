[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path -LiteralPath $RepoRoot).Path
$file = Join-Path $repo 'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs'
$project = Join-Path $repo 'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj'

if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw ('Missing file: ' + $file) }
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) { throw ('Missing file: ' + $project) }

$content = Get-Content -LiteralPath $file -Raw
$required = @(
    'using Azure.Identity;',
    'using Azure.Security.KeyVault.Secrets;',
    'using Migration.Connectors.Targets.Bynder.Clients;',
    'IConfiguration configuration,',
    'ReadBynderTaxonomyAsync(credential, request, configuration, cancellationToken)',
    'ResolveCredentialValueAsync(',
    'ReadKeyVaultSecretByNameAsync(',
    'new BynderRestClient(baseUrl!, clientId!, clientSecret!, scopes!)',
    'optionObject.ToJsonString(JsonOptions)'
)
foreach ($marker in $required) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing required marker: ' + $marker)
    }
}

$forbidden = @(
    'ReadBynderTaxonomyAsync(credential, request, cancellationToken)',
    'Bynder credentials require baseUrl and accessToken/apiToken',
    'configuration).ConfigureAwait(false),'
)
foreach ($marker in $forbidden) {
    if ($content.IndexOf($marker, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Forbidden stale marker found: ' + $marker)
    }
}

[xml]$xml = Get-Content -LiteralPath $project -Raw
$refs = @($xml.SelectNodes("//*[local-name()='PackageReference']"))
foreach ($ref in $refs) {
    if ($ref.HasAttribute('Version')) {
        throw ('Inline PackageReference Version attribute found: ' + $ref.GetAttribute('Include'))
    }
}

Write-Host 'Bynder taxonomy Key Vault compile repair validation passed.'
