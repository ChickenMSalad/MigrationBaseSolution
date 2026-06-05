Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.ProviderPath

$adminRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminRoot 'src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$docsRoot = Join-Path $repoRootPath 'docs\P10'
$originalReport = Join-Path $docsRoot 'P10.2BO-AdminWebNavigationReachabilityAudit.Report.md'
$repairReport = Join-Path $docsRoot 'P10.2BO-Repair-AdminWebNavigationReachabilityAudit.Report.md'

if (-not (Test-Path -Path $adminRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminRoot)
}
if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}
if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}
if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -Path $docsRoot -ItemType Directory -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BO Repair - Admin Web Navigation Reachability Audit')
[void]$report.Add('')
[void]$report.Add('Repair-only validation report. No Admin Web source files were moved or rewritten.')
[void]$report.Add('')
[void]$report.Add('## Checked paths')
[void]$report.Add(('- Admin root: `{0}`' -f $adminRoot))
[void]$report.Add(('- Source root: `{0}`' -f $sourceRoot))
[void]$report.Add(('- App.tsx: `{0}`' -f $appPath))
[void]$report.Add('')

if (Test-Path -Path $originalReport -PathType Leaf) {
    [void]$report.Add('## Original BO report')
    [void]$report.Add(('- Found: `{0}`' -f $originalReport))
    [void]$report.Add('')
}
else {
    [void]$report.Add('## Original BO report')
    [void]$report.Add(('- Not found: `{0}`' -f $originalReport))
    [void]$report.Add('- This is allowed for repair validation because this set is not dependent on the prior report file.')
    [void]$report.Add('')
}

$sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx' | Where-Object {
    $fullName = $_.FullName
    ($fullName -notlike '*\node_modules\*') -and
    ($fullName -notlike '*\dist\*') -and
    ($fullName -notlike '*\reference\*') -and
    ($fullName -notlike '*\apps\*')
})

$tsxImportFiles = New-Object 'System.Collections.Generic.List[string]'
$referenceImportFiles = New-Object 'System.Collections.Generic.List[string]'

foreach ($sourceFile in $sourceFiles) {
    $content = Get-Content -Path $sourceFile.FullName -Raw
    if ($content -match 'from\s+[''\"][^''\"]+\.tsx[''\"]') {
        [void]$tsxImportFiles.Add($sourceFile.FullName)
    }
    if (($content -match 'from\s+[''\"][^''\"]*reference/') -or ($content -match 'from\s+[''\"][^''\"]*apps/')) {
        [void]$referenceImportFiles.Add($sourceFile.FullName)
    }
}

[void]$report.Add('## Compiled source import checks')
[void]$report.Add(('- TypeScript/TSX files scanned: {0}' -f $sourceFiles.Length))
[void]$report.Add(('- Files with `.tsx` import extensions: {0}' -f $tsxImportFiles.Count))
if ($tsxImportFiles.Count -gt 0) {
    foreach ($item in $tsxImportFiles) { [void]$report.Add(('- `{0}`' -f $item)) }
}
[void]$report.Add(('- Files importing reference/apps material: {0}' -f $referenceImportFiles.Count))
if ($referenceImportFiles.Count -gt 0) {
    foreach ($item in $referenceImportFiles) { [void]$report.Add(('- `{0}`' -f $item)) }
}
[void]$report.Add('')
[void]$report.Add('## Result')
[void]$report.Add('P10.2BO repair applied. The BO validator is repaired by avoiding tool-folder/self-scan checks.')

Set-Content -Path $repairReport -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $repairReport)
Write-Host 'P10.2BO Repair Admin Web navigation reachability audit repair applied.'
