Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path -Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$tsconfigPath = Join-Path $adminWebRoot 'tsconfig.json'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BJ-AdminWebCompileScopeReferenceHygiene.Report.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root not found: {0}' -f $sourceRoot)
}
if (-not (Test-Path -Path $tsconfigPath -PathType Leaf)) {
    throw ('tsconfig.json not found: {0}' -f $tsconfigPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report not found: {0}' -f $reportPath)
}

$raw = Get-Content -Path $tsconfigPath -Raw
if ([string]::IsNullOrWhiteSpace($raw)) {
    throw ('tsconfig.json is empty: {0}' -f $tsconfigPath)
}
$tsconfig = $raw | ConvertFrom-Json

$propertyNames = @($tsconfig.PSObject.Properties.Name)
if (-not ($propertyNames -contains 'include')) {
    throw 'tsconfig.json is missing include.'
}
if (-not (@($tsconfig.include) -contains 'src')) {
    throw 'tsconfig.json include must contain src.'
}
if (-not ($propertyNames -contains 'exclude')) {
    throw 'tsconfig.json is missing exclude.'
}
$excludeValues = @($tsconfig.exclude)
$requiredExcludes = @('reference', 'apps', 'node_modules', 'dist')
foreach ($required in $requiredExcludes) {
    if (-not ($excludeValues -contains $required)) {
        throw ('tsconfig.json exclude is missing {0}.' -f $required)
    }
}

$sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx')
foreach ($sourceFile in $sourceFiles) {
    $text = Get-Content -Path $sourceFile.FullName -Raw
    if ($null -eq $text) { continue }
    if ($text.IndexOf('/reference/', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('..\reference\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('../reference/', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('reference/apps-migration-admin-ui', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Canonical source file imports or references reference material: {0}' -f $sourceFile.FullName)
    }
}

Write-Host 'P10.2BJ Admin Web compile scope reference hygiene validation passed.'
