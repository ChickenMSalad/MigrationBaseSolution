Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path

$adminWebRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'
$originalToolRoot = Join-Path $repoRootPath 'tools\p10\P10.2CH'
$repairToolRoot = Join-Path $repoRootPath 'tools\p10\P10.2CH-Repair'
$docsRoot = Join-Path $repoRootPath 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2CH-Repair-AdminWebPreviewSmokeHarness.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$runnerPath = Join-Path $originalToolRoot 'Run-P10.2CH-AdminWebPreviewSmoke.ps1'
if (-not (Test-Path -Path $runnerPath -PathType Leaf)) {
    if (-not (Test-Path -Path $originalToolRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $originalToolRoot -Force | Out-Null
    }

    $runnerLines = New-Object System.Collections.Generic.List[string]
    [void]$runnerLines.Add('Set-StrictMode -Version 2.0')
    [void]$runnerLines.Add('$ErrorActionPreference = ''Stop''')
    [void]$runnerLines.Add('')
    [void]$runnerLines.Add('$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path')
    [void]$runnerLines.Add('$repoRoot = Resolve-Path (Join-Path $scriptRoot ''..\..\..'')')
    [void]$runnerLines.Add('$repoRootPath = $repoRoot.Path')
    [void]$runnerLines.Add('$adminWebRoot = Join-Path $repoRootPath ''src\Admin\Migration.Admin.Web''')
    [void]$runnerLines.Add('if (-not (Test-Path -Path $adminWebRoot -PathType Container)) { throw (''Admin Web root was not found: {0}'' -f $adminWebRoot) }')
    [void]$runnerLines.Add('Push-Location $adminWebRoot')
    [void]$runnerLines.Add('try {')
    [void]$runnerLines.Add('    npm run build')
    [void]$runnerLines.Add('    if ($LASTEXITCODE -ne 0) { throw (''npm run build failed with exit code {0}.'' -f $LASTEXITCODE) }')
    [void]$runnerLines.Add('    npm run preview -- --host 127.0.0.1 --port 4173')
    [void]$runnerLines.Add('} finally {')
    [void]$runnerLines.Add('    Pop-Location')
    [void]$runnerLines.Add('}')
    Set-Content -Path $runnerPath -Value $runnerLines -Encoding UTF8
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2CH Repair - Admin Web Preview Smoke Harness')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:u}' -f (Get-Date)))
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('- Repair-only package for P10.2CH preview smoke harness validation.')
[void]$report.Add('- No Admin Web source files were moved or rewritten.')
[void]$report.Add('- Validation now checks runner capability instead of brittle exact command text.')
[void]$report.Add('')
[void]$report.Add('## Paths')
[void]$report.Add('')
[void]$report.Add(('- Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('- Preview runner: `{0}`' -f $runnerPath))
[void]$report.Add(('- Repair tool root: `{0}`' -f $repairToolRoot))

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CH Repair Admin Web preview smoke harness applied.'
