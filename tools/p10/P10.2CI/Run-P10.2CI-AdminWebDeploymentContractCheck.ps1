Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptRoot))
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$artifactRoot = [System.IO.Path]::Combine($repoRoot, 'artifacts', 'p10', 'P10.2CI')
$summaryPath = [System.IO.Path]::Combine($artifactRoot, 'deployment-contract.summary.md')

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -Path $artifactRoot -ItemType Directory -Force | Out-Null
}

$packagePath = [System.IO.Path]::Combine($adminWebRoot, 'package.json')
$vitePath = [System.IO.Path]::Combine($adminWebRoot, 'vite.config.ts')
$envExamplePath = [System.IO.Path]::Combine($adminWebRoot, '.env.local.example')
$distPath = [System.IO.Path]::Combine($adminWebRoot, 'dist')
$distIndexPath = [System.IO.Path]::Combine($distPath, 'index.html')
$assetsPath = [System.IO.Path]::Combine($distPath, 'assets')

$checks = New-Object 'System.Collections.Generic.List[object]'
[void]$checks.Add([pscustomobject]@{ Name = 'package.json exists'; Passed = (Test-Path -Path $packagePath -PathType Leaf); Detail = $packagePath })
[void]$checks.Add([pscustomobject]@{ Name = 'vite.config.ts exists'; Passed = (Test-Path -Path $vitePath -PathType Leaf); Detail = $vitePath })
[void]$checks.Add([pscustomobject]@{ Name = '.env.local.example exists'; Passed = (Test-Path -Path $envExamplePath -PathType Leaf); Detail = $envExamplePath })
[void]$checks.Add([pscustomobject]@{ Name = 'dist/index.html exists'; Passed = (Test-Path -Path $distIndexPath -PathType Leaf); Detail = $distIndexPath })
[void]$checks.Add([pscustomobject]@{ Name = 'dist/assets exists'; Passed = (Test-Path -Path $assetsPath -PathType Container); Detail = $assetsPath })

$assetCount = 0
if (Test-Path -Path $assetsPath -PathType Container) {
    $assetFiles = @(Get-ChildItem -Path $assetsPath -File -ErrorAction Stop)
    $assetCount = $assetFiles.Length
}
[void]$checks.Add([pscustomobject]@{ Name = 'dist/assets has files'; Passed = ($assetCount -gt 0); Detail = ('Asset count: {0}' -f $assetCount) })

if (Test-Path -Path $packagePath -PathType Leaf) {
    $packageText = [System.IO.File]::ReadAllText($packagePath)
    [void]$checks.Add([pscustomobject]@{ Name = 'package build script'; Passed = ($packageText -match '"build"'); Detail = 'package.json script check' })
    [void]$checks.Add([pscustomobject]@{ Name = 'package preview script'; Passed = ($packageText -match '"preview"'); Detail = 'package.json script check' })
}

if (Test-Path -Path $vitePath -PathType Leaf) {
    $viteText = [System.IO.File]::ReadAllText($vitePath)
    [void]$checks.Add([pscustomobject]@{ Name = 'Vite proxy target env'; Passed = ($viteText -match 'VITE_ADMIN_API_PROXY_TARGET'); Detail = 'vite.config.ts env check' })
    [void]$checks.Add([pscustomobject]@{ Name = 'Vite /api proxy'; Passed = ($viteText -match '"/api"'); Detail = 'vite.config.ts proxy check' })
}

$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('# P10.2CI - Admin Web Deployment Contract Check')
[void]$lines.Add('')
[void]$lines.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$lines.Add(('Generated at: `{0}`' -f ([DateTime]::UtcNow.ToString('u'))))
[void]$lines.Add('')
[void]$lines.Add('| Check | Result | Detail |')
[void]$lines.Add('|---|---:|---|')

$failed = New-Object 'System.Collections.Generic.List[string]'
foreach ($check in $checks) {
    $result = 'PASS'
    if (-not $check.Passed) {
        $result = 'FAIL'
        [void]$failed.Add([string]$check.Name)
    }
    [void]$lines.Add(('| {0} | {1} | `{2}` |' -f $check.Name, $result, $check.Detail))
}

[void]$lines.Add('')
if ($failed.Count -eq 0) {
    [void]$lines.Add('Result: deployment contract check passed.')
} else {
    [void]$lines.Add('Result: deployment contract check failed.')
    [void]$lines.Add('')
    [void]$lines.Add('Failed checks:')
    foreach ($name in $failed) {
        [void]$lines.Add(('- {0}' -f $name))
    }
}

[System.IO.File]::WriteAllLines($summaryPath, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Host ('Wrote deployment contract summary: {0}' -f $summaryPath)

if ($failed.Count -gt 0) {
    throw ('Deployment contract check failed with {0} failed check(s). Review {1}.' -f $failed.Count, $summaryPath)
}

Write-Host 'P10.2CI Admin Web deployment contract check passed.'
