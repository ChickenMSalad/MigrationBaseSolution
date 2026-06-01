Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $current = Resolve-Path -LiteralPath $scriptRoot
    while ($null -ne $current) {
        $candidate = Join-Path -Path $current.Path -ChildPath 'src'
        $gitCandidate = Join-Path -Path $current.Path -ChildPath '.git'
        if ((Test-Path -LiteralPath $candidate -PathType Container) -and (Test-Path -LiteralPath $gitCandidate -PathType Container)) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or ($parent -eq $current.Path)) {
            break
        }
        $current = Resolve-Path -LiteralPath $parent
    }

    throw 'Unable to locate repository root from script path.'
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Path
    )
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Expected file missing for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Path
    )
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        throw ('Legacy file still exists for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-ImportSource {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Source
    )
    Assert-FileExists -Label $Label -Path $Path
    $content = Get-Content -LiteralPath $Path -Raw
    $escapedSource = [System.Text.RegularExpressions.Regex]::Escape($Source)
    $pattern = 'from\s+["'']' + $escapedSource + '["'']'
    if (-not [System.Text.RegularExpressions.Regex]::IsMatch($content, $pattern)) {
        throw ('Expected import source missing for {0}: {1}' -f $Label, $Source)
    }
}

function Assert-ImportSourceNotPresent {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Source
    )
    Assert-FileExists -Label $Label -Path $Path
    $content = Get-Content -LiteralPath $Path -Raw
    $escapedSource = [System.Text.RegularExpressions.Regex]::Escape($Source)
    $pattern = 'from\s+["'']' + $escapedSource + '["'']'
    if ([System.Text.RegularExpressions.Regex]::IsMatch($content, $pattern)) {
        throw ('Unexpected legacy import source found for {0}: {1}' -f $Label, $Source)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path -Path $repoRoot -ChildPath (Join-Path -Path 'src' -ChildPath (Join-Path -Path 'Admin' -ChildPath (Join-Path -Path 'Migration.Admin.Web' -ChildPath 'src')))
$featureRoot = Join-Path -Path $adminSrc -ChildPath (Join-Path -Path 'features' -ChildPath (Join-Path -Path 'connectors' -ChildPath 'configuration'))
$pagePath = Join-Path -Path $featureRoot -ChildPath (Join-Path -Path 'pages' -ChildPath 'ConnectorConfiguration.tsx')
$apiPath = Join-Path -Path $featureRoot -ChildPath (Join-Path -Path 'api' -ChildPath 'connectorConfigurationApi.ts')
$typesPath = Join-Path -Path $featureRoot -ChildPath (Join-Path -Path 'types' -ChildPath 'connectorConfiguration.ts')
$appPath = Join-Path -Path $adminSrc -ChildPath 'App.tsx'

Assert-FileExists -Label 'Connector Configuration page' -Path $pagePath
Assert-FileExists -Label 'Connector Configuration API' -Path $apiPath
Assert-FileExists -Label 'Connector Configuration types' -Path $typesPath
Assert-FileMissing -Label 'Connector Configuration legacy page' -Path (Join-Path -Path $adminSrc -ChildPath (Join-Path -Path 'pages' -ChildPath 'ConnectorConfiguration.tsx'))
Assert-FileMissing -Label 'Connector Configuration legacy API' -Path (Join-Path -Path $adminSrc -ChildPath (Join-Path -Path 'api' -ChildPath 'connectorConfigurationApi.ts'))
Assert-FileMissing -Label 'Connector Configuration legacy types' -Path (Join-Path -Path $adminSrc -ChildPath (Join-Path -Path 'types' -ChildPath 'connectorConfiguration.ts'))

Assert-ImportSource -Label 'page API import' -Path $pagePath -Source '../api/connectorConfigurationApi'
Assert-ImportSource -Label 'page Card import' -Path $pagePath -Source '../../../../components/Card'
Assert-ImportSource -Label 'page LoadingError import' -Path $pagePath -Source '../../../../components/LoadingError'
Assert-ImportSource -Label 'page types import' -Path $pagePath -Source '../types/connectorConfiguration'
Assert-ImportSource -Label 'API types import' -Path $apiPath -Source '../types/connectorConfiguration'
Assert-ImportSource -Label 'App ConnectorConfiguration import' -Path $appPath -Source './features/connectors/configuration/pages/ConnectorConfiguration'

Assert-ImportSourceNotPresent -Label 'App legacy ConnectorConfiguration import' -Path $appPath -Source './pages/ConnectorConfiguration'

Write-Host 'P10.2AO Repair4 Admin Web Connector Configuration feature move validation passed.'
