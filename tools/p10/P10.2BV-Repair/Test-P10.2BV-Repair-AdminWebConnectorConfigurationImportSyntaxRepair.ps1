Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$targetPath = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src\features\connectors\configuration\pages\ConnectorConfiguration.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BV-Repair-AdminWebConnectorConfigurationImportSyntaxRepair.md'

if (-not (Test-Path -Path $targetPath -PathType Leaf)) {
    throw ('Connector Configuration page was not found: {0}' -f $targetPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$content = Get-Content -Path $targetPath -Raw
if ($null -eq $content) {
    throw 'ConnectorConfiguration.tsx content could not be read.'
}

$invalidFragments = @(
    'connectorConfigurationApi""',
    'components/Card""',
    'components/LoadingError""',
    '.tsx"',
    ".tsx'"
)
foreach ($fragment in $invalidFragments) {
    if ($content.Contains($fragment)) {
        throw ('Invalid import fragment remains in ConnectorConfiguration.tsx: {0}' -f $fragment)
    }
}

$requiredFragments = @(
    'from "../api/connectorConfigurationApi";',
    'from "../../../../components/Card";',
    'from "../../../../components/LoadingError";'
)
foreach ($fragment in $requiredFragments) {
    if (-not $content.Contains($fragment)) {
        throw ('Expected import fragment is missing from ConnectorConfiguration.tsx: {0}' -f $fragment)
    }
}

$reportContent = Get-Content -Path $reportPath -Raw
if ($null -eq $reportContent -or -not $reportContent.Contains('P10.2BV Repair')) {
    throw 'P10.2BV Repair report does not contain the expected heading.'
}

Write-Host 'P10.2BV Repair validation passed.'
