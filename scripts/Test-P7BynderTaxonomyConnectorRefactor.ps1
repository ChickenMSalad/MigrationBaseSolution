param(
    [Parameter(Mandatory=$true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    throw 'RepoRoot does not exist.'
}

function Read-Text {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw ('File not found: ' + $Path)
    }
    $value = Get-Content -LiteralPath $Path -Raw
    if ($null -eq $value) {
        throw ('File was empty or unreadable: ' + $Path)
    }
    return $value
}

function Require-Marker {
    param(
        [string]$Content,
        [string]$Marker,
        [string]$Description
    )
    if ($Content.IndexOf($Marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing marker for ' + $Description + ': ' + $Marker)
    }
}

function Forbid-Marker {
    param(
        [string]$Content,
        [string]$Marker,
        [string]$Description
    )
    if ($Content.IndexOf($Marker, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Forbidden marker found in ' + $Description + ': ' + $Marker)
    }
}

$endpointPath = Join-Path $RepoRoot 'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs'
$csprojPath = Join-Path $RepoRoot 'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj'
$clientPath = Join-Path $RepoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Clients\BynderRestClient.cs'

$endpoint = Read-Text -Path $endpointPath
$csproj = Read-Text -Path $csprojPath
$client = Read-Text -Path $clientPath

Require-Marker -Content $endpoint -Marker 'using Migration.Connectors.Targets.Bynder.Clients;' -Description 'taxonomy endpoint connector client usage'
Require-Marker -Content $endpoint -Marker 'new BynderRestClient(baseUrl, clientId, clientSecret, scopes)' -Description 'Bynder connector auth path'
Require-Marker -Content $endpoint -Marker 'api/v4/metaproperties/' -Description 'Bynder metaproperties call'
Require-Marker -Content $endpoint -Marker 'Bynder taxonomy generation requires OAuth credential value(s)' -Description 'OAuth credential validation'
Require-Marker -Content $endpoint -Marker 'BynderMetapropertyObjects' -Description 'Bynder response parser'
Require-Marker -Content $endpoint -Marker 'BynderMetapropertyOptions' -Description 'Bynder options parser'
Forbid-Marker -Content $endpoint -Marker 'Bynder credentials require baseUrl and accessToken/apiToken' -Description 'stale token credential validation'
Forbid-Marker -Content $endpoint -Marker 'bearerToken' -Description 'direct bearer token credential path'
Forbid-Marker -Content $endpoint -Marker 'AuthenticationHeaderValue("Bearer"' -Description 'endpoint-local bearer header auth'

Require-Marker -Content $csproj -Marker 'Migration.Connectors.Targets.Bynder.csproj' -Description 'Admin API Bynder connector project reference'
Require-Marker -Content $client -Marker 'NormalizeBaseUrl' -Description 'Bynder REST client base URL normalization'
Require-Marker -Content $client -Marker 'return value.TrimEnd(' -Description 'Bynder REST client trailing slash normalization'

$replacementChar = [char]0xfffd
if ($endpoint.IndexOf($replacementChar) -ge 0 -or $client.IndexOf($replacementChar) -ge 0) {
    throw 'Replacement character corruption found.'
}

Write-Host 'Bynder taxonomy connector refactor validation passed.'
