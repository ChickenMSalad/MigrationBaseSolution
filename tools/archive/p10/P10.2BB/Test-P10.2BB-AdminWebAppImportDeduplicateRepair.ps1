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
        if ([string]::IsNullOrWhiteSpace($parent) -or ($parent -eq $current.Path)) {
            break
        }
        $current = Get-Item -LiteralPath $parent
    }
    throw 'Unable to locate repository root from current directory.'
}

$repoRoot = Get-RepoRoot
$appPath = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BB-AdminWebAppImportDeduplicateRepair.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx missing: {0}' -f $appPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('P10.2BB report missing: {0}' -f $reportPath)
}

$content = Get-Content -Path $appPath -Raw
$canonicalImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";'
$badImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration"";'

if ($content.Contains($badImport)) {
    throw 'Malformed ConnectorConfiguration import remains in App.tsx.'
}

$firstIndex = $content.IndexOf($canonicalImport, [System.StringComparison]::Ordinal)
if ($firstIndex -lt 0) {
    throw 'Canonical ConnectorConfiguration import missing from App.tsx.'
}
$secondIndex = $content.IndexOf($canonicalImport, $firstIndex + $canonicalImport.Length, [System.StringComparison]::Ordinal)
if ($secondIndex -ge 0) {
    throw 'Duplicate ConnectorConfiguration import remains in App.tsx.'
}

$featurePage = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'features', 'connectors', 'configuration', 'pages', 'ConnectorConfiguration.tsx')
if (-not (Test-Path -Path $featurePage -PathType Leaf)) {
    throw ('ConnectorConfiguration feature page missing: {0}' -f $featurePage)
}

Write-Host 'P10.2BB Admin Web App import deduplicate repair validation passed.'
