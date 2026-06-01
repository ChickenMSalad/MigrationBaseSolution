Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while ($null -ne $current -and $current.Length -gt 0) {
        $marker = [System.IO.Path]::Combine($current, 'MigrationBaseSolution.sln')
        if (Test-Path -Path $marker -PathType Leaf) {
            return $current
        }
        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) { break }
        $current = $parent
    }
    throw 'Unable to locate repo root containing MigrationBaseSolution.sln.'
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Label
    )
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file missing for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-FileAbsent {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Label
    )
    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy file still exists for {0}: {1}' -f $Label, $Path)
    }
}

function Read-TextFile {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Get-ImportSources {
    param([Parameter(Mandatory=$true)][string]$Path)
    $content = Read-TextFile -Path $Path
    $matches = [regex]::Matches($content, '(?m)^\s*import\s+[^;]+?\s+from\s+["'']([^"'']+)["'']\s*;')
    $sources = New-Object System.Collections.Generic.List[string]
    foreach ($match in $matches) {
        $sources.Add($match.Groups[1].Value)
    }
    return @($sources.ToArray())
}

function Assert-ImportSourceExists {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$ExpectedSource,
        [Parameter(Mandatory=$true)][string]$Label
    )
    $sources = @(Get-ImportSources -Path $Path)
    if (-not ($sources -contains $ExpectedSource)) {
        throw ('Expected import source missing for {0}: {1}' -f $Label, $ExpectedSource)
    }
}

function Assert-ImportSourceAbsent {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$UnexpectedSource,
        [Parameter(Mandatory=$true)][string]$Label
    )
    $sources = @(Get-ImportSources -Path $Path)
    if ($sources -contains $UnexpectedSource) {
        throw ('Unexpected import source found for {0}: {1}' -f $Label, $UnexpectedSource)
    }
}

function Assert-DoesNotContainLiteralControlCharacters {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Label
    )
    $content = Read-TextFile -Path $Path
    if ($content.IndexOf([char]9) -ge 0) {
        throw ('Unexpected literal tab character found in {0}: {1}' -f $Label, $Path)
    }
    if ($content.IndexOf([char]7) -ge 0) {
        throw ('Unexpected literal alert character found in {0}: {1}' -f $Label, $Path)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'connectors', 'configuration')

$pagePath = [System.IO.Path]::Combine($featureRoot, 'pages', 'ConnectorConfiguration.tsx')
$apiPath = [System.IO.Path]::Combine($featureRoot, 'api', 'connectorConfigurationApi.ts')
$typePath = [System.IO.Path]::Combine($featureRoot, 'types', 'connectorConfiguration.ts')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')

Assert-FileExists -Path $pagePath -Label 'Connector Configuration page'
Assert-FileExists -Path $apiPath -Label 'Connector Configuration API'
Assert-FileExists -Path $typePath -Label 'Connector Configuration types'
Assert-FileExists -Path $appPath -Label 'App.tsx'

Assert-FileAbsent -Path ([System.IO.Path]::Combine($adminSrc, 'pages', 'ConnectorConfiguration.tsx')) -Label 'legacy Connector Configuration page'
Assert-FileAbsent -Path ([System.IO.Path]::Combine($adminSrc, 'api', 'connectorConfigurationApi.ts')) -Label 'legacy Connector Configuration API'
Assert-FileAbsent -Path ([System.IO.Path]::Combine($adminSrc, 'types', 'connectorConfiguration.ts')) -Label 'legacy Connector Configuration types'

Assert-ImportSourceExists -Path $pagePath -ExpectedSource '../api/connectorConfigurationApi' -Label 'page API import source'
Assert-ImportSourceExists -Path $pagePath -ExpectedSource '../../../../components/Card' -Label 'page Card import source'
Assert-ImportSourceExists -Path $pagePath -ExpectedSource '../../../../components/LoadingError' -Label 'page LoadingError import source'
Assert-ImportSourceAbsent -Path $pagePath -UnexpectedSource '../../api/connectorConfigurationApi' -Label 'legacy page API import source'
Assert-ImportSourceAbsent -Path $pagePath -UnexpectedSource '../components/Card' -Label 'legacy page Card import source'
Assert-ImportSourceAbsent -Path $pagePath -UnexpectedSource '../components/LoadingError' -Label 'legacy page LoadingError import source'

$pageContent = Read-TextFile -Path $pagePath
if ($pageContent -match '(?m)^\s*import\s+[^;]+?\s+from\s+["'']\.\./types/connectorConfiguration["'']\s*;') {
    Write-Host 'Connector Configuration page uses local feature types import.'
} else {
    Write-Host 'Connector Configuration page has no local type import; no type import assertion required.'
}

$appContent = Read-TextFile -Path $appPath
if ($appContent.Contains('<ConnectorConfiguration') -and -not $appContent.Contains('./features/connectors/configuration/pages/ConnectorConfiguration')) {
    throw 'App.tsx references ConnectorConfiguration but does not import it from the canonical feature path.'
}
if ($appContent.Contains('./pages/ConnectorConfiguration')) {
    throw 'App.tsx still imports ConnectorConfiguration from ./pages/ConnectorConfiguration.'
}

$apiContent = Read-TextFile -Path $apiPath
if ($apiContent.Contains('adminApiClient')) {
    Assert-ImportSourceExists -Path $apiPath -ExpectedSource '../../../../api/core/adminApiClient' -Label 'API adminApiClient import source'
} else {
    Write-Host 'Connector Configuration API does not use adminApiClient; no client import assertion required.'
}

Assert-DoesNotContainLiteralControlCharacters -Path $pagePath -Label 'Connector Configuration page'
Assert-DoesNotContainLiteralControlCharacters -Path $apiPath -Label 'Connector Configuration API'
Assert-DoesNotContainLiteralControlCharacters -Path $typePath -Label 'Connector Configuration types'
Assert-DoesNotContainLiteralControlCharacters -Path $appPath -Label 'App.tsx'

Write-Host 'P10.2AO Repair2 Admin Web Connector Configuration feature move validation passed.'
