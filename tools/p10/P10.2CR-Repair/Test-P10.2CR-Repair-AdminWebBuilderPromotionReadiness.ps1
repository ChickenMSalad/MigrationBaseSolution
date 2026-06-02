Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if ((Test-Path (Join-Path $current '.git')) -or (Test-Path (Join-Path $current 'MigrationBase.sln'))) {
            return $current
        }
        $parent = Split-Path -Parent $current
        if ($parent -eq $current) { break }
        $current = $parent
    }
    if ($PSScriptRoot) {
        $fromScript = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
        return $fromScript.Path
    }
    throw 'Unable to locate repository root.'
}

$repoRoot = Get-RepoRoot
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CR-Repair-AdminWebBuilderPromotionReadiness.md'
$applyPath = Join-Path $repoRoot 'tools\p10\P10.2CR-Repair\Apply-P10.2CR-Repair-AdminWebBuilderPromotionReadiness.ps1'
$testPath = Join-Path $repoRoot 'tools\p10\P10.2CR-Repair\Test-P10.2CR-Repair-AdminWebBuilderPromotionReadiness.ps1'

if (-not (Test-Path $applyPath)) { throw ('Missing apply script: {0}' -f $applyPath) }
if (-not (Test-Path $testPath)) { throw ('Missing test script: {0}' -f $testPath) }
if (-not (Test-Path $reportPath)) { throw ('Missing report: {0}' -f $reportPath) }

$report = [System.IO.File]::ReadAllText($reportPath)
if (-not $report.Contains('# P10.2CR Repair - Admin Web Builder Promotion Readiness')) { throw 'Report heading missing.' }
if (-not $report.Contains('## Builder Candidate Inventory')) { throw 'Builder candidate inventory section missing.' }
if (-not $report.Contains('## Promotion Guidance')) { throw 'Promotion guidance section missing.' }
if (-not $report.Contains('canonical features')) { throw 'Canonical feature inventory marker missing.' }
if (-not $report.Contains('reference features')) { throw 'Reference feature inventory marker missing.' }

$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
if (-not (Test-Path $adminRoot)) { throw ('Admin Web root missing: {0}' -f $adminRoot) }
if (-not (Test-Path (Join-Path $adminRoot 'src\features'))) { throw 'Canonical feature root missing.' }

Write-Host 'P10.2CR Repair builder promotion readiness validation passed.'
