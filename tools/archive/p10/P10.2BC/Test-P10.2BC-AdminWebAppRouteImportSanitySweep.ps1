Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BC-AdminWebAppRouteImportSanitySweep.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Expected App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected P10.2BC report was not found: {0}' -f $reportPath)
}

$content = Get-Content -Path $appPath -Raw
$badConnectorDoubleQuote = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration"";'
$goodConnectorImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";'

if ($content.IndexOf($badConnectorDoubleQuote, [System.StringComparison]::Ordinal) -ge 0) {
    throw 'Malformed doubled-quote ConnectorConfiguration import is still present in App.tsx.'
}

$connectorImportCount = 0
$scanIndex = 0
while ($scanIndex -lt $content.Length) {
    $foundIndex = $content.IndexOf($goodConnectorImport, $scanIndex, [System.StringComparison]::Ordinal)
    if ($foundIndex -lt 0) { break }
    $connectorImportCount++
    $scanIndex = $foundIndex + $goodConnectorImport.Length
}

if ($connectorImportCount -gt 1) {
    throw ('Duplicate ConnectorConfiguration canonical imports remain in App.tsx: {0}' -f $connectorImportCount)
}

if ($content.IndexOf('apps/migration-admin-ui', [System.StringComparison]::Ordinal) -ge 0) {
    throw 'App.tsx still contains an apps/migration-admin-ui reference.'
}

# Avoid self-scanning and brittle unsafe-pattern checks. This test validates only the intended target state.
Write-Host 'P10.2BC Admin Web App route import sanity sweep validation passed.'
