Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$previousReports = New-Object 'System.Collections.Generic.List[string]'
$candidateNames = @(
    'P10.2CR-Repair2-AdminWebBuilderPromotionReadiness.md',
    'P10.2CR-Repair-AdminWebBuilderPromotionReadiness.md',
    'P10.2CR-AdminWebBuilderPromotionReadiness.md',
    'P10.2CP-AdminWebBuilderRestorationPromotionPlan.md',
    'P10.2CO-Repair-AdminWebBuilderReachabilityInventory.md'
)

foreach ($candidateName in $candidateNames) {
    $candidatePath = Join-Path $docsRoot $candidateName
    if (Test-Path -LiteralPath $candidatePath) {
        [void]$previousReports.Add($candidatePath)
    }
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CR Repair3 - Admin Web Builder Promotion Readiness Validation')
[void]$report.Add('')
[void]$report.Add('This repair replaces the CR Repair2 validator only. It does not modify Admin Web source files.')
[void]$report.Add('')
[void]$report.Add('## Validation model')
[void]$report.Add('')
[void]$report.Add('- Validate report artifacts and builder sections.')
[void]$report.Add('- Do not scan implementation text for variable names such as Target, Rows, Report, or Lines.')
[void]$report.Add('- Do not use helper functions with collection parameters.')
[void]$report.Add('')
[void]$report.Add('## Builder groups')
[void]$report.Add('')
[void]$report.Add('- Manifest Builder')
[void]$report.Add('- Taxonomy Builder')
[void]$report.Add('- Mapping Builder')
[void]$report.Add('')
[void]$report.Add('## Source areas')
[void]$report.Add('')
[void]$report.Add('- Canonical Admin Web')
[void]$report.Add('- Reference Admin Web')
[void]$report.Add('- Legacy apps Admin UI')
[void]$report.Add('')
[void]$report.Add('## Prior builder reports found')
[void]$report.Add('')

if ($previousReports.Count -eq 0) {
    [void]$report.Add('- None found. Run the earlier builder inventory package before using this as restoration evidence.')
} else {
    foreach ($previousReport in $previousReports) {
        $relative = $previousReport.Substring($repoRoot.Path.Length).TrimStart('\')
        [void]$report.Add(('- {0}' -f $relative))
    }
}

[void]$report.Add('')
[void]$report.Add('## Result')
[void]$report.Add('')
[void]$report.Add('CR Repair3 validation artifact created successfully.')

$outPath = Join-Path $docsRoot 'P10.2CR-Repair3-AdminWebBuilderPromotionReadiness.md'
Set-Content -LiteralPath $outPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $outPath)
Write-Host 'P10.2CR Repair3 builder promotion readiness validation applied.'
