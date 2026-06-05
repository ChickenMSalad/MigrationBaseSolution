Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$sourceRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BQ-AdminWebSharedResidualClassification.Report.md'

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$report = Get-Content -Path $reportPath -Raw
if ($null -eq $report) { $report = '' }

$requiredText = @(
    '# P10.2BQ - Admin Web Shared Residual Classification',
    '## Summary',
    '## Flat API Classification',
    '## Flat Type Classification',
    '## Next Cleanup Guidance'
)

foreach ($text in $requiredText) {
    if (-not $report.Contains($text)) {
        throw ('Expected report text missing: {0}' -f $text)
    }
}

$compiledFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx' | Where-Object {
    $full = $_.FullName
    ($full -notlike '*\reference\*') -and
    ($full -notlike '*\node_modules\*') -and
    ($full -notlike '*\dist\*')
})

foreach ($file in $compiledFiles) {
    if (-not (Test-Path -Path $file.FullName -PathType Leaf)) { continue }
    $content = Get-Content -Path $file.FullName -Raw
    if ($null -eq $content) { $content = '' }
    if ($content.Contains(".tsx'" ) -or $content.Contains('.tsx"')) {
        throw ('Extension-bearing .tsx import found in compiled source: {0}' -f $file.FullName)
    }
    if ($content.Contains("from '../reference") -or $content.Contains('from "../reference') -or $content.Contains("from './reference") -or $content.Contains('from "./reference')) {
        throw ('Compiled source imports reference material: {0}' -f $file.FullName)
    }
}

Write-Host 'P10.2BQ Admin Web shared residual classification validation passed.'
