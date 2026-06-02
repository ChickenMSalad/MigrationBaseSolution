Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docsDir = Join-Path $repoRoot 'docs\P10'
$artifactDir = Join-Path $repoRoot 'artifacts\p10\P10.2CK'
$reportPath = Join-Path $artifactDir 'site-up-evidence.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Canonical Admin Web root was not found: {0}' -f $adminWebRoot)
}

New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null

$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('# P10.2CK - Admin Web Site-Up Evidence Snapshot')
[void]$lines.Add('')
[void]$lines.Add(('Generated: {0:O}' -f (Get-Date)))
[void]$lines.Add(('Repository root: `{0}`' -f $repoRoot))
[void]$lines.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$lines.Add('')
[void]$lines.Add('## Canonical Admin Web project')

$projectFiles = @(
    'package.json',
    'package-lock.json',
    'vite.config.ts',
    'tsconfig.json',
    'src\App.tsx',
    'src\components\Layout.tsx'
)
foreach ($relative in $projectFiles) {
    $path = Join-Path $adminWebRoot $relative
    if (Test-Path -Path $path -PathType Leaf) {
        [void]$lines.Add(('- present: `{0}`' -f $relative))
    }
    else {
        [void]$lines.Add(('- missing: `{0}`' -f $relative))
    }
}

[void]$lines.Add('')
[void]$lines.Add('## Production build output')
$distRoot = Join-Path $adminWebRoot 'dist'
$distIndex = Join-Path $distRoot 'index.html'
$distAssets = Join-Path $distRoot 'assets'
if (Test-Path -Path $distIndex -PathType Leaf) {
    [void]$lines.Add('- dist index present')
}
else {
    [void]$lines.Add('- dist index missing; run `npm run build` from Admin Web root')
}
if (Test-Path -Path $distAssets -PathType Container) {
    $assetFiles = @(Get-ChildItem -Path $distAssets -File -ErrorAction SilentlyContinue)
    [void]$lines.Add(('- dist asset file count: {0}' -f $assetFiles.Length))
}
else {
    [void]$lines.Add('- dist assets folder missing')
}

[void]$lines.Add('')
[void]$lines.Add('## Existing P10 Admin Web harnesses')
$harnesses = @(
    'P10.2BU\Run-P10.2BU-AdminWebNpmBuild.ps1',
    'P10.2CC\Run-P10.2CC-AdminWebDevSmoke.ps1',
    'P10.2CD\Run-P10.2CD-AdminApiConnectivitySmoke.ps1',
    'P10.2CE\Run-P10.2CE-LocalStackSmoke.ps1',
    'P10.2CF\Run-P10.2CF-AdminWebProductionBuild.ps1',
    'P10.2CG\Run-P10.2CG-AdminWebRouteSmoke.ps1',
    'P10.2CH\Run-P10.2CH-AdminWebPreviewSmoke.ps1',
    'P10.2CI\Run-P10.2CI-AdminWebDeploymentContractCheck.ps1'
)
foreach ($relative in $harnesses) {
    $path = Join-Path (Join-Path $repoRoot 'tools\p10') $relative
    if (Test-Path -Path $path -PathType Leaf) {
        [void]$lines.Add(('- present: `tools/p10/{0}`' -f ($relative -replace '\\','/')))
    }
    else {
        [void]$lines.Add(('- not found: `tools/p10/{0}`' -f ($relative -replace '\\','/')))
    }
}

[void]$lines.Add('')
[void]$lines.Add('## Suggested next manual sequence')
[void]$lines.Add('')
[void]$lines.Add('1. Run Admin API locally.')
[void]$lines.Add('2. Run Admin Web locally or preview the production build.')
[void]$lines.Add('3. Run API connectivity, route, preview, and deployment contract smoke scripts.')
[void]$lines.Add('4. Capture generated artifacts under `artifacts/p10`.')

Set-Content -Path $reportPath -Value $lines -Encoding UTF8
Write-Host ('Wrote site-up evidence snapshot: {0}' -f $reportPath)
