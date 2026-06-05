Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path

$appPath = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
$docsDir = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsDir, 'P10.2AZ-AdminWebAppImportRepair.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Required App.tsx file was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -ItemType Directory -Path $docsDir | Out-Null
}

$content = Get-Content -Path $appPath -Raw
$original = $content

$badConnectorImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration"";'
$goodConnectorImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";'

$badCount = ([regex]::Matches($content, [regex]::Escape($badConnectorImport))).Count
$goodCount = ([regex]::Matches($content, [regex]::Escape($goodConnectorImport))).Count

if ($badCount -gt 0) {
    if ($goodCount -gt 0) {
        $content = $content.Replace($badConnectorImport + ' ', '')
        $content = $content.Replace($badConnectorImport, '')
    }
    else {
        $content = $content.Replace($badConnectorImport, $goodConnectorImport)
    }
}

# Collapse accidental duplicate canonical ConnectorConfiguration imports while preserving the first occurrence.
$firstIndex = $content.IndexOf($goodConnectorImport, [System.StringComparison]::Ordinal)
if ($firstIndex -lt 0) {
    # Insert after the Connectors catalog import if the import is genuinely absent.
    $anchor = "import { Connectors } from './features/connectors/catalog/pages/Connectors';"
    $anchorIndex = $content.IndexOf($anchor, [System.StringComparison]::Ordinal)
    if ($anchorIndex -lt 0) {
        throw 'Unable to repair App.tsx; Connectors import anchor was not found.'
    }
    $insertAt = $anchorIndex + $anchor.Length
    $content = $content.Insert($insertAt, ' ' + $goodConnectorImport)
    $firstIndex = $content.IndexOf($goodConnectorImport, [System.StringComparison]::Ordinal)
}

$searchStart = $firstIndex + $goodConnectorImport.Length
while ($true) {
    $nextIndex = $content.IndexOf($goodConnectorImport, $searchStart, [System.StringComparison]::Ordinal)
    if ($nextIndex -lt 0) { break }
    $content = $content.Remove($nextIndex, $goodConnectorImport.Length)
    $content = $content.Replace('  ', ' ')
}

if ($content -ne $original) {
    Set-Content -Path $appPath -Value $content -NoNewline -Encoding UTF8
    Write-Host ('Updated App.tsx import repair: {0}' -f $appPath)
}
else {
    Write-Host ('No App.tsx import repair was needed: {0}' -f $appPath)
}

$report = New-Object System.Collections.ArrayList
[void]$report.Add('# P10.2AZ - Admin Web App Import Repair')
[void]$report.Add('')
[void]$report.Add(('App.tsx: `{0}`' -f $appPath))
[void]$report.Add('')
[void]$report.Add(('Malformed ConnectorConfiguration imports before repair: {0}' -f $badCount))
[void]$report.Add(('Canonical ConnectorConfiguration imports before repair: {0}' -f $goodCount))
[void]$report.Add('')
[void]$report.Add('This set repairs only canonical Admin Web App.tsx import drift.')
Set-Content -Path $reportPath -Value ([string[]]$report) -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2AZ Admin Web App import repair applied.'
