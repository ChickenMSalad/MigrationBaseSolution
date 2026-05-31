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
$appPath = [System.IO.Path]::Combine($repoRoot, 'src/Admin/Migration.Admin.Web/src/App.tsx')
$layoutPath = [System.IO.Path]::Combine($repoRoot, 'src/Admin/Migration.Admin.Web/src/components/Layout.tsx')

foreach ($path in @($appPath, $layoutPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw ('Required Admin Web file is missing: {0}' -f $path)
    }
}

$appText = Get-Content -LiteralPath $appPath -Raw
if ($appText.IndexOf('RuntimeDashboard', [System.StringComparison]::Ordinal) -lt 0) {
    $dashboardImport = 'import { Dashboard } from "./pages/Dashboard";'
    $runtimeImport = 'import { RuntimeDashboard } from "./pages/RuntimeDashboard";'
    if ($appText.IndexOf($dashboardImport, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find Dashboard import in Admin Web App.tsx.'
    }
    $appText = $appText.Replace($dashboardImport, ($dashboardImport + [Environment]::NewLine + $runtimeImport))
}

if ($appText.IndexOf('path="/runtime-dashboard"', [System.StringComparison]::Ordinal) -lt 0) {
    $routeText = '<Route path="/runtime-dashboard" element={<RuntimeDashboard />} />'
    if ($appText.IndexOf('</Routes>', [System.StringComparison]::Ordinal) -ge 0) {
        $appText = $appText.Replace('</Routes>', ('  ' + $routeText + [Environment]::NewLine + '      </Routes>'))
    }
    elseif ($appText.IndexOf('<Route path="/mapping-builder"', [System.StringComparison]::Ordinal) -ge 0) {
        $appText = $appText.Replace('<Route path="/mapping-builder"', ($routeText + ' <Route path="/mapping-builder"'))
    }
    else {
        throw 'Unable to find a safe route insertion point in Admin Web App.tsx.'
    }
}

Set-Content -LiteralPath $appPath -Value $appText -Encoding UTF8

$layoutText = Get-Content -LiteralPath $layoutPath -Raw
if ($layoutText.IndexOf('Runtime Dashboard', [System.StringComparison]::Ordinal) -lt 0) {
    if ($layoutText.IndexOf('Activity,', [System.StringComparison]::Ordinal) -ge 0 -and $layoutText.IndexOf('Gauge', [System.StringComparison]::Ordinal) -lt 0) {
        $layoutText = $layoutText.Replace('Activity,', 'Activity, Gauge,')
    }
    elseif ($layoutText.IndexOf('Gauge', [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to add Gauge icon import to Admin Web Layout.tsx.'
    }

    $runsNav = '{ to: "/runs", label: "Runs", icon: Activity }'
    $dashboardNav = '{ to: "/runtime-dashboard", label: "Runtime Dashboard", icon: Gauge }'
    if ($layoutText.IndexOf($runsNav, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find Runs navigation item in Admin Web Layout.tsx.'
    }
    $layoutText = $layoutText.Replace($runsNav, ($runsNav + ', ' + $dashboardNav))
}

Set-Content -LiteralPath $layoutPath -Value $layoutText -Encoding UTF8

Write-Host 'P10.2G Admin Web Runtime Dashboard route/nav wiring applied.'
