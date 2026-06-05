Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($true) {
        if ([string]::IsNullOrWhiteSpace($current)) {
            throw 'Unable to locate repository root from script path.'
        }

        $solutionPath = Join-Path $current 'MigrationBaseSolution.sln'
        $srcPath = Join-Path $current 'src'
        if ((Test-Path -LiteralPath $solutionPath) -and (Test-Path -LiteralPath $srcPath)) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            throw 'Unable to locate repository root from script path.'
        }

        $current = $parent
    }
}

$repoRoot = Get-RepoRoot
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.2CR-Repair2'
$applyScript = Join-Path $toolRoot 'Apply-P10.2CR-Repair2-AdminWebBuilderPromotionReadiness.ps1'
$testScript = Join-Path $toolRoot 'Test-P10.2CR-Repair2-AdminWebBuilderPromotionReadiness.ps1'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CR-Repair2-AdminWebBuilderPromotionReadiness.md'

if (-not (Test-Path -LiteralPath $applyScript)) {
    throw ('Apply script missing: {0}' -f $applyScript)
}
if (-not (Test-Path -LiteralPath $testScript)) {
    throw ('Test script missing: {0}' -f $testScript)
}
if (-not (Test-Path -LiteralPath $reportPath)) {
    throw ('Report missing. Run the apply script first: {0}' -f $reportPath)
}

$applyText = Get-Content -LiteralPath $applyScript -Raw
$testText = Get-Content -LiteralPath $testScript -Raw
$returnArrayToken = ('return ' + '@(')
$arrayAppendToken = ([string][char]43 + '=')
$stringArrayToken = ('[string' + '[]]')
$objectArrayToken = ('[object' + '[]]')
$rowsToken = ('-R' + 'ows')
$linesToken = ('-L' + 'ines')
$reportToken = ('-R' + 'eport')
$targetToken = ('-T' + 'arget')

if ($applyText.Contains($returnArrayToken)) {
    throw 'Apply script contains forbidden return-array wrapper pattern.'
}
if ($applyText.Contains($arrayAppendToken)) {
    throw 'Apply script contains forbidden array append pattern.'
}
if ($applyText.Contains($stringArrayToken)) {
    throw 'Apply script contains forbidden string-array parameter pattern.'
}
if ($applyText.Contains($objectArrayToken)) {
    throw 'Apply script contains forbidden object-array parameter pattern.'
}
if ($applyText.Contains($rowsToken)) {
    throw 'Apply script contains forbidden Rows parameter usage.'
}
if ($applyText.Contains($linesToken)) {
    throw 'Apply script contains forbidden Lines parameter usage.'
}
if ($applyText.Contains($reportToken)) {
    throw 'Apply script contains forbidden Report parameter usage.'
}
if ($applyText.Contains($targetToken)) {
    throw 'Apply script contains forbidden Target parameter usage.'
}
if ($testText.Contains($returnArrayToken)) {
    throw 'Test script contains forbidden return-array wrapper pattern.'
}
if ($testText.Contains($arrayAppendToken)) {
    throw 'Test script contains forbidden array append pattern.'
}

$reportText = Get-Content -LiteralPath $reportPath -Raw
if (-not $reportText.Contains('# P10.2CR Repair2 - Admin Web Builder Promotion Readiness')) {
    throw 'Report header missing.'
}
if (-not $reportText.Contains('## Builder Candidate Inventory')) {
    throw 'Builder candidate inventory section missing.'
}
if (-not $reportText.Contains('## Promotion Readiness')) {
    throw 'Promotion readiness section missing.'
}
if (-not $reportText.Contains('manifest')) {
    throw 'Report does not mention manifest builder term.'
}
if (-not $reportText.Contains('taxonomy')) {
    throw 'Report does not mention taxonomy builder term.'
}
if (-not $reportText.Contains('mapping')) {
    throw 'Report does not mention mapping builder term.'
}

Write-Host 'P10.2CR Repair2 Admin Web builder promotion readiness validated.'
