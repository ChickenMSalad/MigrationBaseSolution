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
    'docs/p10/P10.2K-Admin-Web-Operations-Routes.md',
    'docs/operations/admin-web-operations-routes.md',
    'config-samples/p10-admin-web-operations-routes.sample.json',
    'tools/runtime/Apply-P102AdminWebOperationsRoutes.ps1',
    'src/Admin/Migration.Admin.Web/src/pages/ExecutionSessions.tsx',
    'src/Admin/Migration.Admin.Web/src/pages/FailureRetry.tsx'
)

foreach ($relativePath in $requiredFiles) {
    $parts = $relativePath.Split('/')
    $fullPath = $repoRoot
    foreach ($part in $parts) {
        $fullPath = [System.IO.Path]::Combine($fullPath, $part)
    }
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2K file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools/runtime/Apply-P102AdminWebOperationsRoutes.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $parts = $relativeScript.Split('/')
    $scriptPath = $repoRoot
    foreach ($part in $parts) {
        $scriptPath = [System.IO.Path]::Combine($scriptPath, $part)
    }
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw ('Script file is missing: {0}' -f $relativeScript)
    }
    $parseErrors = $null
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$appPath = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
$layoutPath = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'components', 'Layout.tsx')
$appText = Get-Content -LiteralPath $appPath -Raw
$layoutText = Get-Content -LiteralPath $layoutPath -Raw

foreach ($term in @('ExecutionSessions', 'FailureRetry', 'path="/execution-sessions"', 'path="/failure-retry"')) {
    if ($appText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('App.tsx is missing operations route term: {0}' -f $term)
    }
}

foreach ($term in @('Execution Sessions', 'Failure Retry', 'to: "/execution-sessions"', 'to: "/failure-retry"')) {
    if ($layoutText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Layout.tsx is missing operations navigation term: {0}' -f $term)
    }
}

$configPath = [System.IO.Path]::Combine($repoRoot, 'config-samples', 'p10-admin-web-operations-routes.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'routes', 'pages')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2K config is missing property: {0}' -f $propertyName)
    }
}

Write-Host 'P10.2K Admin Web operations routes validation passed.'
