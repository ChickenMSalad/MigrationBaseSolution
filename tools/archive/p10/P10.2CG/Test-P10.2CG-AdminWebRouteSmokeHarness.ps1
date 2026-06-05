Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
    }

    return (Get-Location).Path
}

$repoRoot = Get-RepoRoot
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$appTsx = Join-Path $adminWebRoot 'src\App.tsx'
$runner = Join-Path $repoRoot 'tools\p10\P10.2CG\Run-P10.2CG-AdminWebRouteSmoke.ps1'
$applyReport = Join-Path $repoRoot 'artifacts\p10\P10.2CG\P10.2CG-AdminWebRouteSmokeHarness.Apply.md'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CG-AdminWebRouteSmokeHarness.md'

$requiredFiles = @(
    [pscustomobject]@{ Label = 'Admin Web App.tsx'; Path = $appTsx },
    [pscustomobject]@{ Label = 'Route smoke runner'; Path = $runner },
    [pscustomobject]@{ Label = 'Apply report'; Path = $applyReport },
    [pscustomobject]@{ Label = 'Documentation'; Path = $docPath }
)

foreach ($item in $requiredFiles) {
    if (-not (Test-Path -Path $item.Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $item.Label, $item.Path)
    }
}

$appText = Get-Content -Path $appTsx -Raw
if ($appText -notmatch '<Route') {
    throw ('App.tsx does not appear to contain route declarations: {0}' -f $appTsx)
}

$runnerText = Get-Content -Path $runner -Raw
if ($runnerText -match '\.tsx[''" ]') {
    throw ('Runner contains an extension-bearing TSX import-like token: {0}' -f $runner)
}

if ($runnerText -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
    throw ('Runner contains unsafe variable interpolation token: {0}' -f $runner)
}

Write-Host 'P10.2CG Admin Web route smoke harness validation passed.'
