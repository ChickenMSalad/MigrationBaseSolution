Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$packageJsonPath = Join-Path $adminRoot 'package.json'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BU-AdminWebNpmDependencyRestoreHygiene.Report.md'
$restoreRunner = Join-Path $scriptRoot 'Run-P10.2BU-AdminWebCleanDependencyRestore.ps1'
$buildRunner = Join-Path $scriptRoot 'Run-P10.2BU-AdminWebNpmBuild.ps1'

$requiredFiles = @(
    [pscustomobject]@{ Path = $packageJsonPath; Label = 'Admin Web package.json' },
    [pscustomobject]@{ Path = $reportPath; Label = 'P10.2BU report' },
    [pscustomobject]@{ Path = $restoreRunner; Label = 'P10.2BU clean dependency restore runner' },
    [pscustomobject]@{ Path = $buildRunner; Label = 'P10.2BU npm build runner' }
)

foreach ($item in $requiredFiles) {
    if (-not (Test-Path -Path $item.Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $item.Label, $item.Path)
    }
}

$packageJson = (Get-Content -Path $packageJsonPath -Raw) | ConvertFrom-Json
$buildScript = ''
if ($null -ne $packageJson.scripts) {
    $buildProp = $packageJson.scripts.PSObject.Properties | Where-Object { $_.Name -eq 'build' } | Select-Object -First 1
    if ($null -ne $buildProp) {
        $buildScript = [string]$buildProp.Value
    }
}
if ([string]::IsNullOrWhiteSpace($buildScript)) {
    throw 'Admin Web package.json does not define a build script.'
}
if ($buildScript.IndexOf('tsc', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw ('Admin Web build script does not run tsc: {0}' -f $buildScript)
}

$hasTypeScript = $false
if ($null -ne $packageJson.dependencies) {
    $hasTypeScript = $hasTypeScript -or (($packageJson.dependencies.PSObject.Properties | Where-Object { $_.Name -eq 'typescript' } | Select-Object -First 1) -ne $null)
}
if ($null -ne $packageJson.devDependencies) {
    $hasTypeScript = $hasTypeScript -or (($packageJson.devDependencies.PSObject.Properties | Where-Object { $_.Name -eq 'typescript' } | Select-Object -First 1) -ne $null)
}
if (-not $hasTypeScript) {
    throw 'Admin Web package.json build uses tsc but does not declare typescript.'
}

Write-Host 'P10.2BU Admin Web npm dependency restore hygiene validation passed.'
