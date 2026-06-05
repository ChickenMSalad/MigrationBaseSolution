Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.ProviderPath

$sourceRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web\src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$repairReport = Join-Path $repoRootPath 'docs\P10\P10.2BO-Repair-AdminWebNavigationReachabilityAudit.Report.md'

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}
if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}
if (-not (Test-Path -Path $repairReport -PathType Leaf)) {
    throw ('Repair report was not found: {0}' -f $repairReport)
}

$reportText = Get-Content -Path $repairReport -Raw
if ($reportText -notmatch 'P10\.2BO Repair') {
    throw ('Repair report did not contain the expected title: {0}' -f $repairReport)
}
if ($reportText -notmatch 'Compiled source import checks') {
    throw ('Repair report did not contain compiled source import checks: {0}' -f $repairReport)
}

$sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx' | Where-Object {
    $fullName = $_.FullName
    ($fullName -notlike '*\node_modules\*') -and
    ($fullName -notlike '*\dist\*') -and
    ($fullName -notlike '*\reference\*') -and
    ($fullName -notlike '*\apps\*')
})

foreach ($sourceFile in $sourceFiles) {
    $content = Get-Content -Path $sourceFile.FullName -Raw
    if ($content -match 'from\s+[''\"][^''\"]+\.tsx[''\"]') {
        throw ('Compiled source has a .tsx import extension in {0}' -f $sourceFile.FullName)
    }
    if (($content -match 'from\s+[''\"][^''\"]*reference/') -or ($content -match 'from\s+[''\"][^''\"]*apps/')) {
        throw ('Compiled source imports reference/apps material in {0}' -f $sourceFile.FullName)
    }
}

Write-Host 'P10.2BO Repair validation passed.'
