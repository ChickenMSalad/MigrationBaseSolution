Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = [System.IO.Path]::Combine($sourceRoot, 'App.tsx')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BD-AdminWebConnectorConfigurationImportHardening.md')

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Required App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$correctImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";'
$malformedImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration"";'
$content = Get-Content -Path $appPath -Raw

if ($content.Contains($malformedImport)) {
    throw ('Malformed ConnectorConfiguration import still exists in {0}' -f $appPath)
}

$parts = $content.Split([string[]]@($correctImport), [System.StringSplitOptions]::None)
$occurrences = $parts.Length - 1

if ($content.Contains('ConnectorConfiguration') -and $occurrences -ne 1) {
    throw ('Expected exactly one canonical ConnectorConfiguration import when referenced; found {0}' -f $occurrences)
}

if ($content.Contains('ConnectorConfiguration""')) {
    throw ('Unexpected doubled quote sequence still exists near ConnectorConfiguration in {0}' -f $appPath)
}

Write-Host 'P10.2BD Admin Web ConnectorConfiguration import hardening validation passed.'
