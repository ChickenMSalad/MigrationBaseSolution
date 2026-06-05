Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docDir = Join-Path $repoRoot 'docs\P10'
$artifactDir = Join-Path $repoRoot 'artifacts\p10\P10.2BU'
$reportPath = Join-Path $docDir 'P10.2BU-AdminWebNpmDependencyRestoreHygiene.Report.md'

if (-not (Test-Path -Path $adminRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminRoot)
}

$packageJsonPath = Join-Path $adminRoot 'package.json'
if (-not (Test-Path -Path $packageJsonPath -PathType Leaf)) {
    throw ('package.json was not found: {0}' -f $packageJsonPath)
}

if (-not (Test-Path -Path $docDir -PathType Container)) {
    New-Item -ItemType Directory -Path $docDir -Force | Out-Null
}
if (-not (Test-Path -Path $artifactDir -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
}

$packageText = Get-Content -Path $packageJsonPath -Raw
$packageJson = $packageText | ConvertFrom-Json

$buildScript = ''
if ($null -ne $packageJson.scripts) {
    $scriptProps = $packageJson.scripts.PSObject.Properties
    $buildProp = $scriptProps | Where-Object { $_.Name -eq 'build' } | Select-Object -First 1
    if ($null -ne $buildProp) {
        $buildScript = [string]$buildProp.Value
    }
}

$hasTypeScriptDeclaration = $false
if ($null -ne $packageJson.dependencies) {
    $hasTypeScriptDeclaration = $hasTypeScriptDeclaration -or (($packageJson.dependencies.PSObject.Properties | Where-Object { $_.Name -eq 'typescript' } | Select-Object -First 1) -ne $null)
}
if ($null -ne $packageJson.devDependencies) {
    $hasTypeScriptDeclaration = $hasTypeScriptDeclaration -or (($packageJson.devDependencies.PSObject.Properties | Where-Object { $_.Name -eq 'typescript' } | Select-Object -First 1) -ne $null)
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BU - Admin Web npm Dependency Restore Hygiene')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminRoot))
[void]$report.Add(('package.json: `{0}`' -f $packageJsonPath))
[void]$report.Add(('Build script: `{0}`' -f $buildScript))
[void]$report.Add(('TypeScript declared: `{0}`' -f $hasTypeScriptDeclaration))
[void]$report.Add('')
[void]$report.Add('## Next local commands')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2BU\Run-P10.2BU-AdminWebCleanDependencyRestore.ps1')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2BU\Run-P10.2BU-AdminWebNpmBuild.ps1')
[void]$report.Add('```')

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BU Admin Web npm dependency restore hygiene applied.'
