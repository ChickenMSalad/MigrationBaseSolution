[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath = (Join-Path $RepoRoot 'config-samples/p10-admin-ui-consolidation-gate.sample.json'),

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $parts = @($RelativePath -split '[\\/]') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    $current = $RepoRoot
    foreach ($part in $parts) {
        $current = [System.IO.Path]::Combine($current, $part)
    }
    return $current
}

function Get-RelativeFileInventory {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RootPath,

        [Parameter(Mandatory = $true)]
        [string] $RelativeRoot
    )

    if (-not (Test-Path -LiteralPath $RootPath -PathType Container)) {
        return @()
    }

    $ignoredSegments = @('node_modules', 'dist', 'build', '.git', '.vite', '.react-router')
    $files = Get-ChildItem -LiteralPath $RootPath -Recurse -File
    $results = @()

    foreach ($file in @($files)) {
        $relative = $file.FullName.Substring($RootPath.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        $segments = @($relative -split '[\\/]')
        $skip = $false
        foreach ($segment in $segments) {
            if ($ignoredSegments -contains $segment) {
                $skip = $true
            }
        }
        if ($skip) {
            continue
        }

        $results += [pscustomobject]@{
            RelativePath = (($RelativeRoot.TrimEnd('/')) + '/' + ($relative -replace '\\','/'))
            Extension = $file.Extension
        }
    }

    return @($results)
}

$configFullPath = $ConfigurationPath
if (-not [System.IO.Path]::IsPathRooted($configFullPath)) {
    $configFullPath = Join-RepoPath -RelativePath $ConfigurationPath
}
if (-not (Test-Path -LiteralPath $configFullPath -PathType Leaf)) {
    throw ('Configuration file is missing: {0}' -f $configFullPath)
}

$config = Get-Content -LiteralPath $configFullPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'reportOutputPath', 'canonicalFeatureFolderRecommendation', 'migratedFeatureSurfaces')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Configuration is missing property: {0}' -f $propertyName)
    }
}

$canonicalPath = [string]$config.canonicalAdminUiPath
$featureSourcePath = [string]$config.featureSourcePath
$canonicalRoot = Join-RepoPath -RelativePath $canonicalPath
$featureSourceRoot = Join-RepoPath -RelativePath $featureSourcePath

if (-not (Test-Path -LiteralPath $canonicalRoot -PathType Container)) {
    throw ('Canonical Admin UI path is missing: {0}' -f $canonicalPath)
}
if (-not (Test-Path -LiteralPath $featureSourceRoot -PathType Container)) {
    throw ('Feature-source Admin UI path is missing: {0}' -f $featureSourcePath)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = [string]$config.reportOutputPath
}
$outputFullPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
    $outputFullPath = Join-RepoPath -RelativePath $OutputPath
}
$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent -PathType Container)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$canonicalInventory = Get-RelativeFileInventory -RootPath $canonicalRoot -RelativeRoot $canonicalPath
$featureSourceInventory = Get-RelativeFileInventory -RootPath $featureSourceRoot -RelativeRoot $featureSourcePath
$canonicalFeaturesPath = Join-RepoPath -RelativePath ($canonicalPath + '/src/features')
$canonicalFeaturesExists = Test-Path -LiteralPath $canonicalFeaturesPath -PathType Container

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P10.2Y Admin UI Consolidation Report')
$lines.Add('')
$lines.Add(('- Generated UTC: {0}' -f ([DateTimeOffset]::UtcNow.ToString('o'))))
$lines.Add(('- Canonical Admin UI: `{0}`' -f $canonicalPath))
$lines.Add(('- Feature-source Admin UI: `{0}`' -f $featureSourcePath))
$lines.Add(('- Recommended canonical feature folder: `{0}`' -f ([string]$config.canonicalFeatureFolderRecommendation)))
$lines.Add('')
$lines.Add('## Inventory counts')
$lines.Add('')
$lines.Add(('- Canonical tracked files: {0}' -f @($canonicalInventory).Count))
$lines.Add(('- Feature-source tracked files: {0}' -f @($featureSourceInventory).Count))
$lines.Add(('- Canonical `src/features` folder exists: {0}' -f $canonicalFeaturesExists))
$lines.Add('')
$lines.Add('## Migrated feature surfaces tracked by this gate')
$lines.Add('')
foreach ($surface in @($config.migratedFeatureSurfaces)) {
    $lines.Add(('- {0}' -f ([string]$surface)))
}
$lines.Add('')
$lines.Add('## Recommendation')
$lines.Add('')
$lines.Add('Keep `src/Admin/Migration.Admin.Web` as the only deployable Admin UI. Continue using `apps/migration-admin-ui` only as feature-source/reference until the remaining feature families are intentionally migrated or explicitly retired.')
$lines.Add('')
$lines.Add('Before adding more Admin Web pages, consider a structural cleanup pass that groups related canonical files under `src/Admin/Migration.Admin.Web/src/features` while preserving current routes and build behavior.')

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('P10.2Y Admin UI consolidation report written to {0}' -f $outputFullPath)
