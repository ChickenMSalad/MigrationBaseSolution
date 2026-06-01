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
    throw 'Unable to find repository root from the current directory.'
}

function Read-TextFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file missing: {0}' -f $Path)
    }
}

function Assert-FileMissing {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy file still exists: {0}' -f $Path)
    }
}

function Assert-ImportSource {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $content = Read-TextFile -Path $Path
    $pattern = '(?m)^\s*import\s+[^;]+\s+from\s+[''\"]' + [Regex]::Escape($Source) + '[''\"]\s*;?\s*$'
    if ($content -notmatch $pattern) {
        throw ('Expected import source missing for {0}: {1}' -f $Label, $Source)
    }
}

function Assert-NoImportSource {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $content = Read-TextFile -Path $Path
    $pattern = '(?m)^\s*import\s+[^;]+\s+from\s+[''\"]' + [Regex]::Escape($Source) + '[''\"]\s*;?\s*$'
    if ($content -match $pattern) {
        throw ('Unexpected legacy import source found for {0}: {1}' -f $Label, $Source)
    }
}

function Assert-NoUnsafeScriptText {
    param([Parameter(Mandatory = $true)][string]$RepoRoot)
    $toolRoot = [System.IO.Path]::Combine($RepoRoot, 'tools', 'p10', 'P10.2AO-Repair3')
    $scripts = @(Get-ChildItem -Path $toolRoot -Filter '*.ps1' -File)
    foreach ($script in $scripts) {
        $content = Read-TextFile -Path $script.FullName
        if ($content -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
            throw ('Unsafe variable-colon interpolation found in script: {0}' -f $script.FullName)
        }
        if ($content -match '@\(\s*@\(') {
            throw ('Nested array validation pattern found in script: {0}' -f $script.FullName)
        }
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'connectors', 'configuration')
$pagePath = [System.IO.Path]::Combine($featureRoot, 'pages', 'ConnectorConfiguration.tsx')
$apiPath = [System.IO.Path]::Combine($featureRoot, 'api', 'connectorConfigurationApi.ts')
$typePath = [System.IO.Path]::Combine($featureRoot, 'types', 'connectorConfiguration.ts')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')

Assert-FileExists -Path $pagePath
Assert-FileExists -Path $apiPath
Assert-FileExists -Path $typePath
Assert-FileMissing -Path ([System.IO.Path]::Combine($adminSrc, 'pages', 'ConnectorConfiguration.tsx'))
Assert-FileMissing -Path ([System.IO.Path]::Combine($adminSrc, 'api', 'connectorConfigurationApi.ts'))
Assert-FileMissing -Path ([System.IO.Path]::Combine($adminSrc, 'types', 'connectorConfiguration.ts'))

Assert-ImportSource -Path $pagePath -Source '../api/connectorConfigurationApi' -Label 'page API import source'
Assert-ImportSource -Path $pagePath -Source '../../../../components/Card' -Label 'page Card import source'
Assert-ImportSource -Path $pagePath -Source '../../../../components/LoadingError' -Label 'page LoadingError import source'
Assert-NoImportSource -Path $pagePath -Source '../components/Card' -Label 'page legacy Card import source'
Assert-NoImportSource -Path $pagePath -Source '../components/LoadingError' -Label 'page legacy LoadingError import source'

$apiContent = Read-TextFile -Path $apiPath
if ($apiContent -match '(?m)^\s*import\s+[^;]+\s+from\s+[''\"][^''\"]*adminApiClient[''\"]\s*;?\s*$') {
    Assert-ImportSource -Path $apiPath -Source '../../../../api/core/adminApiClient' -Label 'API adminApiClient import source'
}

$appContent = Read-TextFile -Path $appPath
$appExpected = '(?m)^\s*import\s+\{\s*ConnectorConfiguration\s*\}\s+from\s+[''\"]\.\/features\/connectors\/configuration\/pages\/ConnectorConfiguration[''\"]\s*;?\s*$'
if ($appContent -notmatch $appExpected) {
    throw 'App.tsx ConnectorConfiguration import is not pointed at the canonical feature page.'
}

Assert-NoUnsafeScriptText -RepoRoot $repoRoot
Write-Host 'P10.2AO Repair3 validation passed.'
