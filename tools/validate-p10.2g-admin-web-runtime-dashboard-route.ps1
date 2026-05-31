[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $toolRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator root.'
}

$repoRoot = Split-Path -Parent $toolRoot
$requiredFiles = @(
    'docs/p10/P10.2G-Admin-Web-Runtime-Dashboard-Route-Wiring.md',
    'docs/operations/admin-ui-canonical-runtime-dashboard-route.md',
    'config-samples/p10-admin-web-runtime-dashboard-route.sample.json',
    'tools/runtime/Apply-P102AdminWebRuntimeDashboardRoute.ps1',
    'src/Admin/Migration.Admin.Web/src/pages/RuntimeDashboard.tsx',
    'src/Admin/Migration.Admin.Web/src/api/runtimeDashboardApi.ts',
    'src/Admin/Migration.Admin.Web/src/types/runtimeDashboard.ts',
    'src/Admin/Migration.Admin.Web/src/App.tsx',
    'src/Admin/Migration.Admin.Web/src/components/Layout.tsx'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = [System.IO.Path]::Combine($repoRoot, $relativePath)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2G file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools/runtime/Apply-P102AdminWebRuntimeDashboardRoute.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = [System.IO.Path]::Combine($repoRoot, $relativeScript)
    $parseErrors = $null
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$appText = Get-Content -LiteralPath ([System.IO.Path]::Combine($repoRoot, 'src/Admin/Migration.Admin.Web/src/App.tsx')) -Raw
foreach ($term in @('RuntimeDashboard', 'path="/runtime-dashboard"')) {
    if ($appText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Admin Web App.tsx is missing runtime dashboard route term: {0}' -f $term)
    }
}

$layoutText = Get-Content -LiteralPath ([System.IO.Path]::Combine($repoRoot, 'src/Admin/Migration.Admin.Web/src/components/Layout.tsx')) -Raw
foreach ($term in @('Runtime Dashboard', '/runtime-dashboard')) {
    if ($layoutText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Admin Web Layout.tsx is missing runtime dashboard navigation term: {0}' -f $term)
    }
}

$configPath = [System.IO.Path]::Combine($repoRoot, 'config-samples/p10-admin-web-runtime-dashboard-route.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'routePath', 'pageComponent')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2G config is missing property: {0}' -f $propertyName)
    }
}

if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2G canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}

Write-Host 'P10.2G Admin Web runtime dashboard route validation passed.'
