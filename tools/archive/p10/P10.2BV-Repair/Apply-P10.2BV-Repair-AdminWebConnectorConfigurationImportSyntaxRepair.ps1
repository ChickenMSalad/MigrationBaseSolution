Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$targetPath = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src\features\connectors\configuration\pages\ConnectorConfiguration.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BV-Repair-AdminWebConnectorConfigurationImportSyntaxRepair.md'

if (-not (Test-Path -Path $targetPath -PathType Leaf)) {
    throw ('Connector Configuration page was not found: {0}' -f $targetPath)
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BV Repair - Admin Web Connector Configuration Import Syntax Repair')
[void]$report.Add('')
[void]$report.Add(('Target: `{0}`' -f $targetPath))
[void]$report.Add('')

$content = Get-Content -Path $targetPath -Raw
if ($null -eq $content) {
    $content = ''
}

$original = $content

$replacements = @(
    [pscustomobject]@{ From = 'from "../api/connectorConfigurationApi"";'; To = 'from "../api/connectorConfigurationApi";'; Label = 'Connector Configuration API import' },
    [pscustomobject]@{ From = 'from "../../../../components/Card"";'; To = 'from "../../../../components/Card";'; Label = 'Card import' },
    [pscustomobject]@{ From = 'from "../../../../components/LoadingError"";'; To = 'from "../../../../components/LoadingError";'; Label = 'LoadingError import' }
)

foreach ($replacement in $replacements) {
    if ($content.Contains($replacement.From)) {
        $content = $content.Replace($replacement.From, $replacement.To)
        [void]$report.Add(('- Fixed {0}.' -f $replacement.Label))
    }
    elseif ($content.Contains($replacement.To)) {
        [void]$report.Add(('- Already valid {0}.' -f $replacement.Label))
    }
    else {
        [void]$report.Add(('- Did not find {0}; no replacement applied.' -f $replacement.Label))
    }
}

# Repair any accidental doubled quote before a semicolon on import-from lines in this page only.
$lines = @($content -split "`r?`n", -1)
$updatedLines = New-Object 'System.Collections.Generic.List[string]'
$lineChangedCount = 0
foreach ($line in $lines) {
    if ($null -eq $line) {
        [void]$updatedLines.Add('')
        continue
    }

    $updatedLine = $line
    if ($updatedLine.TrimStart().StartsWith('import ') -and $updatedLine.Contains(' from ') -and $updatedLine.Contains('"";')) {
        $nextLine = $updatedLine.Replace('"";', '";')
        if ($nextLine -ne $updatedLine) {
            $lineChangedCount++
            $updatedLine = $nextLine
        }
    }

    [void]$updatedLines.Add($updatedLine)
}
$content = [string]::Join([Environment]::NewLine, $updatedLines.ToArray())
if ($lineChangedCount -gt 0) {
    [void]$report.Add(('- Normalized {0} import line(s) with doubled quote terminators.' -f $lineChangedCount))
}

if ($content -ne $original) {
    Set-Content -Path $targetPath -Value $content -Encoding UTF8
    [void]$report.Add('')
    [void]$report.Add('Updated ConnectorConfiguration.tsx.')
}
else {
    [void]$report.Add('')
    [void]$report.Add('No ConnectorConfiguration.tsx changes were required.')
}

# Validate the target file after repair.
$validated = Get-Content -Path $targetPath -Raw
if ($validated.Contains('connectorConfigurationApi""')) {
    throw 'Connector Configuration API import still has a doubled quote terminator.'
}
if ($validated.Contains('components/Card""')) {
    throw 'Card import still has a doubled quote terminator.'
}
if ($validated.Contains('components/LoadingError""')) {
    throw 'LoadingError import still has a doubled quote terminator.'
}
if ($validated.Contains('.tsx"') -or $validated.Contains(".tsx'")) {
    throw 'A .tsx extension import remains in ConnectorConfiguration.tsx.'
}

$reportDirectory = Split-Path -Parent $reportPath
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -Path $reportDirectory -ItemType Directory -Force | Out-Null
}
Set-Content -Path $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BV Repair Connector Configuration import syntax repair applied.'
