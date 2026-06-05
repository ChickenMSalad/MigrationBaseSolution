Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptRoot))

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactsRoot = Join-Path $repoRoot 'artifacts\p10\P10.3I'
$reportPath = Join-Path $docsRoot 'P10.3I-AdminWebProductionHardeningInventory.md'
$artifactReportPath = Join-Path $artifactsRoot 'production-hardening-inventory.md'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}
if (-not (Test-Path -LiteralPath $artifactsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
}

$packageJsonPath = Join-Path $adminWebRoot 'package.json'
$viteConfigPath = Join-Path $adminWebRoot 'vite.config.ts'
$envExamplePath = Join-Path $adminWebRoot '.env.local.example'
$srcRoot = Join-Path $adminWebRoot 'src'
$appPath = Join-Path $srcRoot 'App.tsx'
$layoutPath = Join-Path $srcRoot 'components\Layout.tsx'
$distPath = Join-Path $adminWebRoot 'dist'
$distIndexPath = Join-Path $distPath 'index.html'

$packageContent = ''
if (Test-Path -LiteralPath $packageJsonPath -PathType Leaf) {
    $packageContent = Get-Content -LiteralPath $packageJsonPath -Raw
}
$viteContent = ''
if (Test-Path -LiteralPath $viteConfigPath -PathType Leaf) {
    $viteContent = Get-Content -LiteralPath $viteConfigPath -Raw
}
$envContent = ''
if (Test-Path -LiteralPath $envExamplePath -PathType Leaf) {
    $envContent = Get-Content -LiteralPath $envExamplePath -Raw
}

$sourceFiles = @()
if (Test-Path -LiteralPath $srcRoot -PathType Container) {
    $sourceFiles = @(Get-ChildItem -LiteralPath $srcRoot -Recurse -File -Include *.ts,*.tsx | Where-Object { $_.FullName -notmatch '\\node_modules\\|\\dist\\|\\reference\\' })
}

$referenceImportCount = 0
$hardcodedLocalhostCount = 0
$envUsageCount = 0
$consoleCount = 0
$fetchCount = 0
foreach ($file in $sourceFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -like '*reference/*' -or $content -like '*../reference*' -or $content -like '*../../reference*') { $referenceImportCount++ }
    if ($content -like '*localhost:*') { $hardcodedLocalhostCount++ }
    if ($content -like '*import.meta.env*' -or $content -like '*VITE_*') { $envUsageCount++ }
    if ($content -like '*console.error*' -or $content -like '*console.warn*' -or $content -like '*console.log*') { $consoleCount++ }
    if ($content -like '*fetch(*') { $fetchCount++ }
}

$checks = New-Object 'System.Collections.Generic.List[object]'
$checks.Add([pscustomobject]@{ Area='Package'; Check='package.json exists'; Status=$(if (Test-Path -LiteralPath $packageJsonPath -PathType Leaf) { 'PASS' } else { 'FAIL' }); Evidence=$packageJsonPath }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Package'; Check='build script present'; Status=$(if ($packageContent -like '*"build"*') { 'PASS' } else { 'WARN' }); Evidence='package.json scripts' }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Package'; Check='dev script present'; Status=$(if ($packageContent -like '*"dev"*') { 'PASS' } else { 'WARN' }); Evidence='package.json scripts' }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Runtime config'; Check='.env.local.example exists'; Status=$(if (Test-Path -LiteralPath $envExamplePath -PathType Leaf) { 'PASS' } else { 'WARN' }); Evidence=$envExamplePath }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Runtime config'; Check='VITE/Admin API config documented'; Status=$(if ($envContent -like '*VITE*' -or $viteContent -like '*VITE*') { 'PASS' } else { 'WARN' }); Evidence='.env.local.example / vite.config.ts' }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Routing'; Check='App.tsx exists'; Status=$(if (Test-Path -LiteralPath $appPath -PathType Leaf) { 'PASS' } else { 'FAIL' }); Evidence=$appPath }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Navigation'; Check='Layout.tsx exists'; Status=$(if (Test-Path -LiteralPath $layoutPath -PathType Leaf) { 'PASS' } else { 'WARN' }); Evidence=$layoutPath }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Build output'; Check='dist/index.html exists after local build'; Status=$(if (Test-Path -LiteralPath $distIndexPath -PathType Leaf) { 'PASS' } else { 'WARN' }); Evidence=$distIndexPath }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Compile scope'; Check='compiled source does not import reference tree'; Status=$(if ($referenceImportCount -eq 0) { 'PASS' } else { 'FAIL' }); Evidence=('{0} source file(s)' -f $referenceImportCount) }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Deployment'; Check='source hardcoded localhost count'; Status=$(if ($hardcodedLocalhostCount -eq 0) { 'PASS' } else { 'WARN' }); Evidence=('{0} source file(s)' -f $hardcodedLocalhostCount) }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Observability'; Check='console usage inventory'; Status='INFO'; Evidence=('{0} source file(s)' -f $consoleCount) }) | Out-Null
$checks.Add([pscustomobject]@{ Area='API client'; Check='fetch usage inventory'; Status='INFO'; Evidence=('{0} source file(s)' -f $fetchCount) }) | Out-Null
$checks.Add([pscustomobject]@{ Area='Runtime config'; Check='env usage inventory'; Status='INFO'; Evidence=('{0} source file(s)' -f $envUsageCount) }) | Out-Null

$failCount = @($checks | Where-Object { $_.Status -eq 'FAIL' }).Count
$warnCount = @($checks | Where-Object { $_.Status -eq 'WARN' }).Count
$passCount = @($checks | Where-Object { $_.Status -eq 'PASS' }).Count
$infoCount = @($checks | Where-Object { $_.Status -eq 'INFO' }).Count

$report = New-Object 'System.Collections.Generic.List[string]'
$report.Add('# P10.3I - Admin Web Production Hardening Inventory') | Out-Null
$report.Add('') | Out-Null
$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o')))) | Out-Null
$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot)) | Out-Null
$report.Add('') | Out-Null
$report.Add('## Summary') | Out-Null
$report.Add('') | Out-Null
$report.Add(('- PASS: {0}' -f $passCount)) | Out-Null
$report.Add(('- WARN: {0}' -f $warnCount)) | Out-Null
$report.Add(('- FAIL: {0}' -f $failCount)) | Out-Null
$report.Add(('- INFO: {0}' -f $infoCount)) | Out-Null
$report.Add('') | Out-Null
$report.Add('## Inventory') | Out-Null
$report.Add('') | Out-Null
$report.Add('| Area | Check | Status | Evidence |') | Out-Null
$report.Add('| --- | --- | --- | --- |') | Out-Null
foreach ($check in $checks) {
    $report.Add(('| {0} | {1} | {2} | `{3}` |' -f $check.Area, $check.Check, $check.Status, $check.Evidence)) | Out-Null
}
$report.Add('') | Out-Null
$report.Add('## Production hardening notes') | Out-Null
$report.Add('') | Out-Null
$report.Add('- Keep Admin Web API base URL environment-driven; do not hardcode local development ports in source.') | Out-Null
$report.Add('- Keep reference and legacy app trees out of compiled TypeScript scope.') | Out-Null
$report.Add('- Treat WARN rows as pre-production follow-up items, not site-up blockers unless they break deployment configuration.') | Out-Null
$report.Add('- Keep missing legacy builder parity deferred to the recovered-commit feature restore workstream.') | Out-Null

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Set-Content -LiteralPath $artifactReportPath -Value $report.ToArray() -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host ('Wrote artifact report: {0}' -f $artifactReportPath)
Write-Host 'P10.3I Admin Web production hardening inventory applied.'
