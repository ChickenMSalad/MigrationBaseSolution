Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docsDir = Join-Path $repoRoot 'docs\P10'
$artifactsDir = Join-Path $repoRoot 'artifacts\p10\P10.2CF'
$reportPath = Join-Path $docsDir 'P10.2CF-AdminWebDeploymentReadiness.Report.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

New-Item -ItemType Directory -Force -Path $docsDir | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null

$requiredFiles = @(
    [pscustomobject]@{ Label = 'package.json'; Path = (Join-Path $adminWebRoot 'package.json') },
    [pscustomobject]@{ Label = 'package-lock.json'; Path = (Join-Path $adminWebRoot 'package-lock.json') },
    [pscustomobject]@{ Label = 'vite.config.ts'; Path = (Join-Path $adminWebRoot 'vite.config.ts') },
    [pscustomobject]@{ Label = 'tsconfig.json'; Path = (Join-Path $adminWebRoot 'tsconfig.json') },
    [pscustomobject]@{ Label = 'index.html'; Path = (Join-Path $adminWebRoot 'index.html') },
    [pscustomobject]@{ Label = 'src'; Path = (Join-Path $adminWebRoot 'src') }
)

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CF - Admin Web Deployment Readiness Report')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:O}' -f (Get-Date)))
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add('')
[void]$report.Add('## Required build surface')
[void]$report.Add('')

foreach ($item in $requiredFiles) {
    $exists = Test-Path -Path $item.Path
    $status = 'Missing'
    if ($exists) { $status = 'Present' }
    [void]$report.Add(('- {0}: {1} - `{2}`' -f $item.Label, $status, $item.Path))
}

$referenceRoot = Join-Path $adminWebRoot 'reference\apps-migration-admin-ui'
$referenceExists = Test-Path -Path $referenceRoot -PathType Container
[void]$report.Add('')
[void]$report.Add('## Reference material')
[void]$report.Add('')
if ($referenceExists) {
    [void]$report.Add(('- Reference area present: `{0}`' -f $referenceRoot))
} else {
    [void]$report.Add('- Reference area not present.')
}

$distRoot = Join-Path $adminWebRoot 'dist'
[void]$report.Add('')
[void]$report.Add('## Production build artifact status')
[void]$report.Add('')
if (Test-Path -Path (Join-Path $distRoot 'index.html') -PathType Leaf) {
    [void]$report.Add(('- Existing dist index found: `{0}`' -f (Join-Path $distRoot 'index.html')))
} else {
    [void]$report.Add('- Existing dist output not found yet. Run the P10.2CF production build verifier when ready.')
}

[void]$report.Add('')
[void]$report.Add('## Next recommended verification')
[void]$report.Add('')
[void]$report.Add('Run:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CF\Run-P10.2CF-AdminWebProductionBuild.ps1')
[void]$report.Add('```')

$report | Set-Content -Path $reportPath -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CF Admin Web deployment readiness applied.'
