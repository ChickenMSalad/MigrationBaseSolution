[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $toolRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator root.'
}

$repoRoot = Split-Path -Parent $toolRoot

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $RelativePath
    )

    $fullPath = $repoRoot
    foreach ($part in $RelativePath.Split('/')) {
        if (-not [string]::IsNullOrWhiteSpace($part)) {
            $fullPath = [System.IO.Path]::Combine($fullPath, $part)
        }
    }
    return $fullPath
}

$checks = @(
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/types/capacityForecast.ts'
        Terms = @('CapacityForecastSummary', 'CapacityForecastMetric', 'CapacityForecastRecommendation')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/api/capacityForecastApi.ts'
        Terms = @('/api/operational/capacity/forecast', 'getCapacityForecast', 'adminApiClient')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/pages/CapacityForecast.tsx'
        Terms = @('Capacity Forecast', 'getCapacityForecast', 'recommendations')
    },
    [pscustomobject]@{
        Path = 'docs/p10/P10.2W-Admin-Web-Capacity-Forecast-Client.md'
        Terms = @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui')
    },
    [pscustomobject]@{
        Path = 'docs/operations/admin-web-capacity-forecast-client.md'
        Terms = @('feature-source', 'canonical')
    },
    [pscustomobject]@{
        Path = 'config-samples/p10-admin-web-capacity-forecast-client.sample.json'
        Terms = @('P10.2W', 'capacityForecast')
    }
)

foreach ($check in $checks) {
    $pathProperty = $check.PSObject.Properties['Path']
    $termsProperty = $check.PSObject.Properties['Terms']

    if ($null -eq $pathProperty -or $null -eq $termsProperty) {
        throw 'Validator check entry is malformed.'
    }

    $relativePath = [string]$pathProperty.Value
    if ([string]::IsNullOrWhiteSpace($relativePath)) {
        throw 'Validator check entry has an empty Path.'
    }

    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required file is missing: {0}' -f $relativePath)
    }

    $text = Get-Content -LiteralPath $fullPath -Raw
    foreach ($term in @($termsProperty.Value)) {
        $termText = [string]$term
        if ([string]::IsNullOrWhiteSpace($termText)) {
            throw ('Validator check entry has an empty expected term for file: {0}' -f $relativePath)
        }
        if ($text.IndexOf($termText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ('File {0} is missing expected term: {1}' -f $relativePath, $termText)
        }
    }
}

Write-Host 'P10.2W Admin Web capacity forecast client validation passed.'
