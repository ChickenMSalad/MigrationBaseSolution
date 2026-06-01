Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))

$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$appTsx = [System.IO.Path]::Combine($adminWebRoot, 'src', 'App.tsx')
$originalReport = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BF-AdminWebAppRouteTargetVerification.Report.md')
$repairReport = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BF-Repair-AdminWebAppRouteTargetVerification.md')

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $appTsx -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appTsx)
}

if (-not (Test-Path -Path $originalReport -PathType Leaf)) {
    throw ('Original P10.2BF route verification report was not found: {0}' -f $originalReport)
}

if (-not (Test-Path -Path $repairReport -PathType Leaf)) {
    throw ('P10.2BF repair report was not found: {0}' -f $repairReport)
}

$content = Get-Content -Path $repairReport -Raw
if ([string]::IsNullOrWhiteSpace($content)) {
    throw ('P10.2BF repair report is empty: {0}' -f $repairReport)
}

Write-Host 'P10.2BF Repair validation passed.'
