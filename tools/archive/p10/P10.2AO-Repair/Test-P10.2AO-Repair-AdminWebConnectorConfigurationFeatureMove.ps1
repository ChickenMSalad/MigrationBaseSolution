Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = Get-Location
    while ($null -ne $current) {
        $candidate = [System.IO.Path]::Combine($current.Path, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current.Path
        }

        $parent = Split-Path -Path $current.Path -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Get-Item -LiteralPath $parent
    }

    throw 'Unable to locate repository root from current directory.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string[]] $Segments
    )

    $path = $Root
    foreach ($segment in $Segments) {
        $path = [System.IO.Path]::Combine($path, $segment)
    }

    return $path
}

function Read-AllTextStrict {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file missing: {0}' -f $Path)
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected {0} file missing: {1}' -f $Label, $Path)
    }
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy {0} file still exists: {1}' -f $Label, $Path)
    }
}

function Assert-ImportSource {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $ImportedName,
        [Parameter(Mandatory = $true)][string] $ExpectedSource,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Read-AllTextStrict -Path $Path
    $escapedName = [System.Text.RegularExpressions.Regex]::Escape($ImportedName)
    $escapedSource = [System.Text.RegularExpressions.Regex]::Escape($ExpectedSource)
    $pattern = '(?m)^\s*import\s+(?:type\s+)?\{[^\r\n]*\b' + $escapedName + '\b[^\r\n]*\}\s+from\s+["'']' + $escapedSource + '["''];?\s*$'
    if (-not [System.Text.RegularExpressions.Regex]::IsMatch($content, $pattern)) {
        throw ('Expected import missing for {0}: {1} from {2}' -f $Label, $ImportedName, $ExpectedSource)
    }
}

function Assert-NoImportSource {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $UnexpectedSource,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Read-AllTextStrict -Path $Path
    $escapedSource = [System.Text.RegularExpressions.Regex]::Escape($UnexpectedSource)
    $pattern = '(?m)^\s*import\s+(?:type\s+)?[^\r\n]+\s+from\s+["'']' + $escapedSource + '["''];?\s*$'
    if ([System.Text.RegularExpressions.Regex]::IsMatch($content, $pattern)) {
        throw ('Unexpected legacy import found for {0}: {1}' -f $Label, $UnexpectedSource)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features', 'connectors', 'configuration')

$page = Join-RepoPath -Root $featureRoot -Segments @('pages', 'ConnectorConfiguration.tsx')
$api = Join-RepoPath -Root $featureRoot -Segments @('api', 'connectorConfigurationApi.ts')
$types = Join-RepoPath -Root $featureRoot -Segments @('types', 'connectorConfiguration.ts')
$app = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')

Assert-FileExists -Path $page -Label 'Connector Configuration page'
Assert-FileExists -Path $api -Label 'Connector Configuration API'
Assert-FileExists -Path $types -Label 'Connector Configuration types'
Assert-FileMissing -Path (Join-RepoPath -Root $adminSrc -Segments @('pages', 'ConnectorConfiguration.tsx')) -Label 'Connector Configuration page'
Assert-FileMissing -Path (Join-RepoPath -Root $adminSrc -Segments @('api', 'connectorConfigurationApi.ts')) -Label 'Connector Configuration API'
Assert-FileMissing -Path (Join-RepoPath -Root $adminSrc -Segments @('types', 'connectorConfiguration.ts')) -Label 'Connector Configuration types'

Assert-ImportSource -Path $page -ImportedName 'connectorConfigurationApi' -ExpectedSource '../api/connectorConfigurationApi' -Label 'page API import'
Assert-ImportSource -Path $page -ImportedName 'Card' -ExpectedSource '../../../../components/Card' -Label 'page Card import'
Assert-ImportSource -Path $page -ImportedName 'LoadingError' -ExpectedSource '../../../../components/LoadingError' -Label 'page LoadingError import'
Assert-ImportSource -Path $page -ImportedName 'ConnectorConfigurationSummary' -ExpectedSource '../types/connectorConfiguration' -Label 'page type import'
Assert-ImportSource -Path $api -ImportedName 'ConnectorConfigurationSummary' -ExpectedSource '../types/connectorConfiguration' -Label 'API type import'
Assert-ImportSource -Path $app -ImportedName 'ConnectorConfiguration' -ExpectedSource './features/connectors/configuration/pages/ConnectorConfiguration' -Label 'App.tsx page import'

Assert-NoImportSource -Path $page -UnexpectedSource '../components/Card' -Label 'page Card import'
Assert-NoImportSource -Path $page -UnexpectedSource '../components/LoadingError' -Label 'page LoadingError import'

$apiContent = Read-AllTextStrict -Path $api
if ($apiContent -notmatch 'connectorConfigurationApi') {
    throw 'Connector Configuration API export was not found.'
}
if ($apiContent -notmatch 'fetch\(' -and $apiContent -notmatch 'adminApiClient') {
    throw 'Connector Configuration API does not contain a recognized request implementation.'
}

Write-Host 'P10.2AO Repair Admin Web Connector Configuration feature move validation passed.'
