Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BT-AdminWebLocalBuildVerification.Report.md')
$runnerPath = [System.IO.Path]::Combine($scriptRoot, 'Run-P10.2BT-AdminWebNpmBuild.ps1')

$requiredPaths = @(
    [pscustomobject]@{ Label = 'Admin Web root'; Path = $adminWebRoot; Kind = 'Container' },
    [pscustomobject]@{ Label = 'package.json'; Path = [System.IO.Path]::Combine($adminWebRoot, 'package.json'); Kind = 'Leaf' },
    [pscustomobject]@{ Label = 'tsconfig.json'; Path = [System.IO.Path]::Combine($adminWebRoot, 'tsconfig.json'); Kind = 'Leaf' },
    [pscustomobject]@{ Label = 'vite.config.ts'; Path = [System.IO.Path]::Combine($adminWebRoot, 'vite.config.ts'); Kind = 'Leaf' },
    [pscustomobject]@{ Label = 'BT report'; Path = $reportPath; Kind = 'Leaf' },
    [pscustomobject]@{ Label = 'BT build runner'; Path = $runnerPath; Kind = 'Leaf' }
)

foreach ($item in $requiredPaths) {
    if (-not (Test-Path -Path $item.Path -PathType $item.Kind)) {
        throw ('Required path missing ({0}): {1}' -f $item.Label, $item.Path)
    }
}

$package = (Get-Content -Path ([System.IO.Path]::Combine($adminWebRoot, 'package.json')) -Raw) | ConvertFrom-Json
if ($null -eq $package.PSObject.Properties['scripts']) {
    throw 'package.json scripts object is missing.'
}
if ($null -eq $package.scripts.PSObject.Properties['build']) {
    throw 'package.json build script is missing.'
}

$reportText = Get-Content -Path $reportPath -Raw
if ($reportText.IndexOf('Admin Web Local Build Verification Report', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'BT report header was not found.'
}
if ($reportText.IndexOf('npm run build', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'BT report does not mention npm run build.'
}

$runnerText = Get-Content -Path $runnerPath -Raw
if ($runnerText.IndexOf('npm run build', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'BT build runner does not execute npm run build.'
}
if ($runnerText.IndexOf('.tsx''', [System.StringComparison]::Ordinal) -ge 0 -or $runnerText.IndexOf('.tsx"', [System.StringComparison]::Ordinal) -ge 0) {
    throw 'BT build runner contains an extension-bearing TSX import-like token.'
}

Write-Host 'P10.2BT Admin Web local build verification checks passed.'
