Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Find-RepoRoot {
    param([string]$StartPath)
    $current = Resolve-Path -Path $StartPath
    while ($null -ne $current) {
        $candidate = Join-Path -Path $current.Path -ChildPath 'MigrationBaseSolution.sln'
        if (Test-Path -Path $candidate -PathType Leaf) { return $current.Path }
        $parent = Split-Path -Path $current.Path -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) { break }
        $current = Resolve-Path -Path $parent
    }
    throw 'Unable to locate repository root containing MigrationBaseSolution.sln.'
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Find-RepoRoot -StartPath $scriptRoot
$adminRoot = Join-Path -Path $repoRoot -ChildPath 'src/Admin/Migration.Admin.Web'
$sourceRoot = Join-Path -Path $adminRoot -ChildPath 'src'
$reportPath = Join-Path -Path $repoRoot -ChildPath 'docs/P10/P10.2BS-AdminWebConsolidationBuildSurfaceReview.Report.md'
if (-not (Test-Path -Path $reportPath -PathType Leaf)) { throw ('Expected report not found: {0}' -f $reportPath) }
$report = Get-Content -Path $reportPath -Raw
$requiredSections = @('# P10.2BS - Admin Web Consolidation Build Surface Review','## Build Surface Files','## Package Script Posture','## TypeScript Scope Posture','## Vite Proxy Posture','## Canonical Feature Root','## Compiled Source Import Hygiene')
foreach ($section in $requiredSections) { if (-not $report.Contains($section)) { throw ('Expected report section missing: {0}' -f $section) } }
$requiredFiles = @((Join-Path -Path $adminRoot -ChildPath 'package.json'),(Join-Path -Path $adminRoot -ChildPath 'tsconfig.json'),(Join-Path -Path $adminRoot -ChildPath 'vite.config.ts'),(Join-Path -Path $sourceRoot -ChildPath 'App.tsx'))
foreach ($file in $requiredFiles) { if (-not (Test-Path -Path $file -PathType Leaf)) { throw ('Required Admin Web file missing: {0}' -f $file) } }
$sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx' -ErrorAction Stop)
foreach ($sourceFile in $sourceFiles) {
    $lines = @(Get-Content -Path $sourceFile.FullName)
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $trimmed = $line.Trim()
        if (-not ($trimmed.StartsWith('import '))) { continue }
        if ($trimmed.Contains(".tsx'") -or $trimmed.Contains('.tsx"')) { throw ('Extension-bearing .tsx import found in {0}: {1}' -f $sourceFile.FullName, $trimmed) }
        if ($trimmed.Contains('/reference/') -or $trimmed.Contains('../reference') -or $trimmed.Contains('reference/')) { throw ('Compiled source imports reference material in {0}: {1}' -f $sourceFile.FullName, $trimmed) }
        if ($trimmed.Contains('/apps/') -or $trimmed.Contains('../apps') -or $trimmed.Contains('apps/migration-admin-ui')) { throw ('Compiled source imports apps material in {0}: {1}' -f $sourceFile.FullName, $trimmed) }
    }
}
Write-Host 'P10.2BS Admin Web consolidation build surface review validation passed.'
