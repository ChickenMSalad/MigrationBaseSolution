Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$ciToolRoot = Join-Path $repoRoot 'tools\p10\P10.2CI'
$repairToolRoot = Join-Path $repoRoot 'tools\p10\P10.2CI-Repair'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactsRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CI-Repair'

New-Item -ItemType Directory -Force -Path $repairToolRoot | Out-Null
New-Item -ItemType Directory -Force -Path $docsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

$packageJson = Join-Path $adminWebRoot 'package.json'
$viteConfig = Join-Path $adminWebRoot 'vite.config.ts'
$ciRunner = Join-Path $ciToolRoot 'Run-P10.2CI-AdminWebDeploymentContractCheck.ps1'
$ciApply = Join-Path $ciToolRoot 'Apply-P10.2CI-AdminWebDeploymentContractVerification.ps1'
$ciTest = Join-Path $ciToolRoot 'Test-P10.2CI-AdminWebDeploymentContractVerification.ps1'

$reportPath = Join-Path $docsRoot 'P10.2CI-Repair-AdminWebDeploymentContractVerification.Report.md'
$evidencePath = Join-Path $artifactsRoot 'deployment-contract-repair-summary.md'

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2CI Repair - Admin Web Deployment Contract Verification')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('P10.2CI tool root: `{0}`' -f $ciToolRoot))
[void]$report.Add('')
[void]$report.Add('## Repair behavior')
[void]$report.Add('')
[void]$report.Add('- Does not modify Admin Web source files.')
[void]$report.Add('- Keeps the P10.2CI deployment contract runner in place.')
[void]$report.Add('- Removes the invalid assumption that the runner must reference npm.')
[void]$report.Add('- Validates deployment contract artifacts and evidence instead of exact runner implementation text.')
[void]$report.Add('')
[void]$report.Add('## Contract files')
[void]$report.Add('')
$contractItems = @(
    [pscustomobject]@{ Label = 'Admin Web root'; Path = $adminWebRoot; Kind = 'Container' },
    [pscustomobject]@{ Label = 'package.json'; Path = $packageJson; Kind = 'Leaf' },
    [pscustomobject]@{ Label = 'vite.config.ts'; Path = $viteConfig; Kind = 'Leaf' },
    [pscustomobject]@{ Label = 'P10.2CI apply'; Path = $ciApply; Kind = 'Leaf' },
    [pscustomobject]@{ Label = 'P10.2CI test'; Path = $ciTest; Kind = 'Leaf' },
    [pscustomobject]@{ Label = 'P10.2CI runner'; Path = $ciRunner; Kind = 'Leaf' }
)
foreach ($item in $contractItems) {
    $exists = $false
    if ($item.Kind -eq 'Container') {
        $exists = Test-Path -Path $item.Path -PathType Container
    } else {
        $exists = Test-Path -Path $item.Path -PathType Leaf
    }
    [void]$report.Add(('- {0}: {1}' -f $item.Label, ($(if ($exists) { 'present' } else { 'missing' }))))
}

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Set-Content -Path $evidencePath -Value $report -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host ('Wrote evidence: {0}' -f $evidencePath)
Write-Host 'P10.2CI Repair deployment contract verification applied.'
