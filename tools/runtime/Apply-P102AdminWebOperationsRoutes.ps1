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
$layoutPath = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'components', 'Layout.tsx')

foreach ($requiredPath in @($appPath, $layoutPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw ('Required Admin Web file is missing: {0}' -f $requiredPath)
    }
}

$appText = Get-Content -LiteralPath $appPath -Raw

if ($appText.IndexOf('from "./pages/ExecutionSessions"', [System.StringComparison]::Ordinal) -lt 0) {
    $anchor = 'import { RuntimeRunDetail } from "./pages/RuntimeRunDetail";'
    if ($appText.IndexOf($anchor, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find RuntimeRunDetail import anchor in App.tsx.'
    }
    $replacement = $anchor + ' import { ExecutionSessions } from "./pages/ExecutionSessions"; import { FailureRetry } from "./pages/FailureRetry";'
    $appText = $appText.Replace($anchor, $replacement)
}

if ($appText.IndexOf('path="/execution-sessions"', [System.StringComparison]::Ordinal) -lt 0) {
    $fallbackPattern = '<Route\s+path="\*"\s+element=\{<Navigate\s+to="/"\s+replace\s*/>\}\s*/>'
    $newRoutes = '<Route path="/execution-sessions" element={<ExecutionSessions />} /> <Route path="/failure-retry" element={<FailureRetry />} /> '
    $match = [System.Text.RegularExpressions.Regex]::Match($appText, $fallbackPattern)
    if (-not $match.Success) {
        throw 'Unable to find fallback route insertion point in App.tsx.'
    }
    $appText = $appText.Substring(0, $match.Index) + $newRoutes + $match.Value + $appText.Substring($match.Index + $match.Length)
}

Set-Content -LiteralPath $appPath -Value $appText -Encoding UTF8

$layoutText = Get-Content -LiteralPath $layoutPath -Raw

if ($layoutText.IndexOf('Workflow', [System.StringComparison]::Ordinal) -lt 0) {
    $importAnchor = 'Gauge,'
    if ($layoutText.IndexOf($importAnchor, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find Gauge import anchor in Layout.tsx.'
    }
    $layoutText = $layoutText.Replace($importAnchor, 'Gauge, Workflow, RefreshCcw,')
}

if ($layoutText.IndexOf('to: "/execution-sessions"', [System.StringComparison]::Ordinal) -lt 0) {
    $navAnchor = '{ to: "/runtime-dashboard", label: "Runtime Dashboard", icon: Gauge },'
    if ($layoutText.IndexOf($navAnchor, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find Runtime Dashboard navigation anchor in Layout.tsx.'
    }
    $navReplacement = $navAnchor + ' { to: "/execution-sessions", label: "Execution Sessions", icon: Workflow }, { to: "/failure-retry", label: "Failure Retry", icon: RefreshCcw },'
    $layoutText = $layoutText.Replace($navAnchor, $navReplacement)
}

Set-Content -LiteralPath $layoutPath -Value $layoutText -Encoding UTF8

Write-Host 'Admin Web operations routes applied.'
