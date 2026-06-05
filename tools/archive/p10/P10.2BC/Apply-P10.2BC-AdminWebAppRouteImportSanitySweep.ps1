Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')
$docsDir = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsDir, 'P10.2BC-AdminWebAppRouteImportSanitySweep.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Expected App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -Path $docsDir -ItemType Directory -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BC - Admin Web App Route Import Sanity Sweep')
[void]$report.Add('')
[void]$report.Add(('Repository root: {0}' -f $repoRoot))
[void]$report.Add(('App.tsx: {0}' -f $appPath))
[void]$report.Add('')

$content = Get-Content -Path $appPath -Raw
$original = $content

$badConnectorDoubleQuote = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration"";'
$goodConnectorImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";'

if ($content.IndexOf($badConnectorDoubleQuote, [System.StringComparison]::Ordinal) -ge 0) {
    $content = $content.Replace($badConnectorDoubleQuote, $goodConnectorImport)
    [void]$report.Add('- Repaired malformed ConnectorConfiguration import with doubled quote terminator.')
} else {
    [void]$report.Add('- Malformed doubled-quote ConnectorConfiguration import was not present.')
}

# Some previous repair attempts could leave the same import repeated on a single physical line.
# This loop tokenizes import statements by semicolon and de-duplicates exact import statements only.
$segments = @($content -split ';')
$rebuilt = New-Object 'System.Collections.Generic.List[string]'
$seenImports = New-Object 'System.Collections.Generic.HashSet[string]'
$removedDuplicates = 0

foreach ($segmentRaw in $segments) {
    $segment = [string]$segmentRaw
    if ($segment.Length -eq 0) {
        continue
    }

    $candidate = $segment.Trim()
    if ($candidate.Length -eq 0) {
        continue
    }

    if ($candidate.StartsWith('import ', [System.StringComparison]::Ordinal)) {
        if ($seenImports.Contains($candidate)) {
            $removedDuplicates++
            continue
        }
        [void]$seenImports.Add($candidate)
    }

    [void]$rebuilt.Add($candidate)
}

if ($rebuilt.Count -eq 0) {
    throw 'App.tsx content rebuild unexpectedly produced no statements.'
}

$content = [string]::Join('; ', $rebuilt.ToArray())
if (-not $content.EndsWith(';', [System.StringComparison]::Ordinal)) {
    $content = $content + ';'
}

[void]$report.Add(('- Removed exact duplicate import statements: {0}' -f $removedDuplicates))

if ($content -ne $original) {
    Set-Content -Path $appPath -Value $content -Encoding UTF8
    [void]$report.Add('- Updated App.tsx.')
} else {
    [void]$report.Add('- App.tsx already matched the expected import hygiene state.')
}

# Report route/page import status without failing the apply step.
$freshContent = Get-Content -Path $appPath -Raw
$connectorImportCount = 0
$connectorImportText = $goodConnectorImport
$scanIndex = 0
while ($scanIndex -lt $freshContent.Length) {
    $foundIndex = $freshContent.IndexOf($connectorImportText, $scanIndex, [System.StringComparison]::Ordinal)
    if ($foundIndex -lt 0) { break }
    $connectorImportCount++
    $scanIndex = $foundIndex + $connectorImportText.Length
}
[void]$report.Add(('- ConnectorConfiguration canonical import occurrences: {0}' -f $connectorImportCount))

$appsReferenceCount = 0
$appsText = 'apps/migration-admin-ui'
$appsIndex = 0
while ($appsIndex -lt $freshContent.Length) {
    $foundAppsIndex = $freshContent.IndexOf($appsText, $appsIndex, [System.StringComparison]::Ordinal)
    if ($foundAppsIndex -lt 0) { break }
    $appsReferenceCount++
    $appsIndex = $foundAppsIndex + $appsText.Length
}
[void]$report.Add(('- App.tsx apps/migration-admin-ui references: {0}' -f $appsReferenceCount))

Set-Content -Path $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BC Admin Web App route import sanity sweep applied.'
