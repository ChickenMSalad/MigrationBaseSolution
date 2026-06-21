[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-TextFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Required file missing: ' + $Path)
    }
    return Get-Content -LiteralPath $Path -Raw
}

function Require-Marker {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Marker,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if ($Content.IndexOf($Marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw ($Description + ' missing marker: ' + $Marker)
    }
}

function Forbid-Marker {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Marker,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if ($Content.IndexOf($Marker, [System.StringComparison]::Ordinal) -ge 0) {
        throw ($Description + ' contains forbidden marker: ' + $Marker)
    }
}

if (-not (Test-Path -LiteralPath $RepoRoot -PathType Container)) {
    throw ('RepoRoot does not exist: ' + $RepoRoot)
}

$taxonomyEndpointPath = Join-Path $RepoRoot 'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs'
$credentialsPath = Join-Path $RepoRoot 'src\Admin\Migration.Admin.Web\src\features\security\credentials\pages\Credentials.tsx'
$taxonomyPagePath = Join-Path $RepoRoot 'src\Admin\Migration.Admin.Web\src\features\platform\builders\taxonomy\pages\TaxonomyBuilder.tsx'
$mappingPagePath = Join-Path $RepoRoot 'src\Admin\Migration.Admin.Web\src\features\platform\builders\mapping\pages\MappingBuilder.tsx'
$loadingErrorPath = Join-Path $RepoRoot 'src\Admin\Migration.Admin.Web\src\components\LoadingError.tsx'
$manifestBuilderPath = Join-Path $RepoRoot 'src\Admin\Migration.Admin.Web\src\features\platform\builders\manifest\pages\ManifestBuilder.tsx'

$taxonomyEndpoint = Read-TextFile -Path $taxonomyEndpointPath
Require-Marker -Content $taxonomyEndpoint -Marker 'RequestBynderAccessTokenAsync' -Description 'Taxonomy endpoint'
Require-Marker -Content $taxonomyEndpoint -Marker 'ClientId' -Description 'Taxonomy endpoint'
Require-Marker -Content $taxonomyEndpoint -Marker 'ClientSecret' -Description 'Taxonomy endpoint'
Require-Marker -Content $taxonomyEndpoint -Marker '/v6/authentication/oauth2/token' -Description 'Taxonomy endpoint'
Forbid-Marker -Content $taxonomyEndpoint -Marker 'Bynder credentials require baseUrl and accessToken/apiToken.' -Description 'Taxonomy endpoint'

$credentials = Read-TextFile -Path $credentialsPath
Require-Marker -Content $credentials -Marker 'displayCredentialRows' -Description 'Credentials page'
Require-Marker -Content $credentials -Marker 'secret: false' -Description 'Credentials page'
Require-Marker -Content $credentials -Marker 'Client ID' -Description 'Credentials page'
Forbid-Marker -Content $credentials -Marker '<JsonBlock value={item.values} />' -Description 'Credentials page'

$webFiles = @($credentialsPath, $taxonomyPagePath, $mappingPagePath, $loadingErrorPath, $manifestBuilderPath)
foreach ($path in $webFiles) {
    $content = Read-TextFile -Path $path
    $chars = $content.ToCharArray()
    foreach ($ch in $chars) {
        if ([int][char]$ch -gt 127) {
            throw ('Non-ASCII UI character found in ' + $path + '. Character code: ' + ([int][char]$ch))
        }
    }
}

$staleBackup = Join-Path $RepoRoot 'src\Admin\Migration.Admin.Web\src\features\platform\builders\taxonomy\pages\TaxonomyBuilder.tsx.p7-taxonomy-json-post.bak'
if (Test-Path -LiteralPath $staleBackup -PathType Leaf) {
    throw ('Stale taxonomy backup file still present: ' + $staleBackup)
}

Write-Host 'Bynder taxonomy credential/UI repair validation passed.'
