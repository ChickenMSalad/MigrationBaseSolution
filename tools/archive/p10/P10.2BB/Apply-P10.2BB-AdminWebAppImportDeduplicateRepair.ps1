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
if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}

$content = Get-Content -Path $appPath -Raw
$original = $content
$canonicalImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";'
$badImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration"";'

if ($content.Contains($badImport)) {
    $content = $content.Replace($badImport, $canonicalImport)
}

$singleQuotedImport = "import { ConnectorConfiguration } from './features/connectors/configuration/pages/ConnectorConfiguration';"
$content = $content.Replace($singleQuotedImport, $canonicalImport)

$firstIndex = $content.IndexOf($canonicalImport, [System.StringComparison]::Ordinal)
if ($firstIndex -lt 0) {
    throw 'Canonical ConnectorConfiguration import was not found after normalization.'
}

$searchStart = $firstIndex + $canonicalImport.Length
while ($true) {
    $nextIndex = $content.IndexOf($canonicalImport, $searchStart, [System.StringComparison]::Ordinal)
    if ($nextIndex -lt 0) {
        break
    }
    $before = $content.Substring(0, $nextIndex)
    $after = $content.Substring($nextIndex + $canonicalImport.Length)
    $content = $before + $after
    $searchStart = $firstIndex + $canonicalImport.Length
}

# Normalize accidental doubled semicolons/spaces introduced by removing duplicate imports on one-line App.tsx.
while ($content.Contains(';  ;')) {
    $content = $content.Replace(';  ;', ';')
}
while ($content.Contains('; ;')) {
    $content = $content.Replace('; ;', ';')
}

if ($content -ne $original) {
    Set-Content -Path $appPath -Value $content -Encoding UTF8
    Write-Host ('Updated App.tsx ConnectorConfiguration import hygiene: {0}' -f $appPath)
} else {
    Write-Host ('App.tsx ConnectorConfiguration import hygiene already clean: {0}' -f $appPath)
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BB - Admin Web App Import Deduplicate Repair')
[void]$report.Add('')
[void]$report.Add('Updated canonical Admin Web App.tsx import hygiene for ConnectorConfiguration.')
[void]$report.Add('')
[void]$report.Add(('App.tsx: {0}' -f $appPath))
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BB-AdminWebAppImportDeduplicateRepair.md')
$reportDir = Split-Path -Path $reportPath -Parent
if (-not (Test-Path -Path $reportDir -PathType Container)) {
    New-Item -Path $reportDir -ItemType Directory -Force | Out-Null
}
Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BB Admin Web App import deduplicate repair applied.'
