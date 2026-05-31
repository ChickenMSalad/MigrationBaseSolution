[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$appPath = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')

if (-not (Test-Path -LiteralPath $appPath -PathType Leaf)) {
    throw ('Admin Web App.tsx is missing: {0}' -f $appPath)
}

$text = Get-Content -LiteralPath $appPath -Raw
$changed = $false

if ($text.IndexOf('RuntimeRunDetail', [System.StringComparison]::Ordinal) -lt 0) {
    $dashboardImport = 'import { RuntimeDashboard } from "./pages/RuntimeDashboard";'
    $detailImport = 'import { RuntimeRunDetail } from "./pages/RuntimeRunDetail";'

    if ($text.IndexOf($dashboardImport, [System.StringComparison]::Ordinal) -ge 0) {
        $text = $text.Replace($dashboardImport, ($dashboardImport + [Environment]::NewLine + $detailImport))
        $changed = $true
    }
    else {
        $runDetailImport = 'import { RunDetail } from "./pages/RunDetail";'
        if ($text.IndexOf($runDetailImport, [System.StringComparison]::Ordinal) -lt 0) {
            throw 'Unable to find a safe import insertion point for RuntimeRunDetail.'
        }
        $text = $text.Replace($runDetailImport, ($runDetailImport + [Environment]::NewLine + $detailImport))
        $changed = $true
    }
}

$hasDetailRoute = $text.IndexOf('/runtime-dashboard/:runId', [System.StringComparison]::OrdinalIgnoreCase) -ge 0
if (-not $hasDetailRoute) {
    $routeLine = '        <Route path="/runtime-dashboard/:runId" element={<RuntimeRunDetail />} />'
    $dashboardRoute = '        <Route path="/runtime-dashboard" element={<RuntimeDashboard />} />'

    if ($text.IndexOf($dashboardRoute, [System.StringComparison]::Ordinal) -ge 0) {
        $text = $text.Replace($dashboardRoute, ($dashboardRoute + [Environment]::NewLine + $routeLine))
        $changed = $true
    }
    else {
        $compactDashboardRoute = '<Route path="/runtime-dashboard" element={<RuntimeDashboard />} />'
        if ($text.IndexOf($compactDashboardRoute, [System.StringComparison]::Ordinal) -lt 0) {
            throw 'Unable to find the P10.2G runtime dashboard route. Apply P10.2G first or wire RuntimeRunDetail manually.'
        }
        $text = $text.Replace($compactDashboardRoute, ($compactDashboardRoute + [Environment]::NewLine + $routeLine))
        $changed = $true
    }
}

if ($changed) {
    Set-Content -LiteralPath $appPath -Value $text -Encoding UTF8
    Write-Host 'Added Admin Web runtime run-detail route.'
}
else {
    Write-Host 'Admin Web runtime run-detail route already present.'
}
