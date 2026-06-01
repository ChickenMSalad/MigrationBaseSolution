Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path

$appPath = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AZ-AdminWebAppImportRepair.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Required App.tsx file was not found: {0}' -f $appPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report file was not found: {0}' -f $reportPath)
}

$content = Get-Content -Path $appPath -Raw
$badConnectorImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration"";'
$goodConnectorImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";'

$badCount = ([regex]::Matches($content, [regex]::Escape($badConnectorImport))).Count
if ($badCount -ne 0) {
    throw ('Malformed ConnectorConfiguration import still exists in App.tsx: {0}' -f $badCount)
}

$goodCount = ([regex]::Matches($content, [regex]::Escape($goodConnectorImport))).Count
if ($goodCount -ne 1) {
    throw ('Expected exactly one canonical ConnectorConfiguration import in App.tsx; found {0}' -f $goodCount)
}

$duplicatePattern = 'ConnectorConfiguration""'
if ($content.Contains($duplicatePattern)) {
    throw 'App.tsx still contains a duplicate quote in the ConnectorConfiguration import.'
}

Write-Host 'P10.2AZ Admin Web App import repair validation passed.'
