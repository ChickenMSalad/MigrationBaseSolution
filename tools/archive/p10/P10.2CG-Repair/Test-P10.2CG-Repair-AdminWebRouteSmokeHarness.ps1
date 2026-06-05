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
$originalApplyReport = Join-Path $repoRoot 'artifacts\p10\P10.2CG\P10.2CG-AdminWebRouteSmokeHarness.Apply.md'
$repairReport = Join-Path $repoRoot 'artifacts\p10\P10.2CG-Repair\P10.2CG-Repair-AdminWebRouteSmokeHarness.Apply.md'
$repairDoc = Join-Path $repoRoot 'docs\P10\P10.2CG-Repair-AdminWebRouteSmokeHarness.md'

$requiredFiles = @(
    [pscustomobject]@{ Label = 'Admin Web App.tsx'; Path = $appTsx },
    [pscustomobject]@{ Label = 'P10.2CG route smoke runner'; Path = $runner },
    [pscustomobject]@{ Label = 'P10.2CG apply report'; Path = $originalApplyReport },
    [pscustomobject]@{ Label = 'P10.2CG repair report'; Path = $repairReport },
    [pscustomobject]@{ Label = 'P10.2CG repair documentation'; Path = $repairDoc }
)

foreach ($item in $requiredFiles) {
    if (-not (Test-Path -Path $item.Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $item.Label, $item.Path)
    }
}

$appText = Get-Content -Path $appTsx -Raw
if ($appText -notmatch '<Route') {
    throw ('App.tsx does not contain route declarations: {0}' -f $appTsx)
}

$runnerText = Get-Content -Path $runner -Raw
if ($runnerText -notmatch 'Invoke-WebRequest') {
    throw ('Route smoke runner does not contain Invoke-WebRequest: {0}' -f $runner)
}

if ($runnerText -notmatch 'P10.2CG Admin Web route smoke') {
    throw ('Route smoke runner does not appear to be the P10.2CG runner: {0}' -f $runner)
}

$repairText = Get-Content -Path $repairReport -Raw
if ($repairText -notmatch 'validator only') {
    throw ('Repair report did not include expected validator-only scope: {0}' -f $repairReport)
}

Write-Host 'P10.2CG Repair Admin Web route smoke harness validation passed.'
