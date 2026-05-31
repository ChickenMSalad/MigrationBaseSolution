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

$requiredRelativePaths = @(
    'docs/p10/P10.2H-Admin-Web-Runtime-Run-Detail-Client.md',
    'docs/operations/admin-web-runtime-run-detail-client.md',
    'config-samples/p10-admin-web-runtime-run-detail-client.sample.json',
    'src/Admin/Migration.Admin.Web/src/pages/RuntimeRunDetail.tsx',
    'tools/runtime/Apply-P102AdminWebRuntimeRunDetailRoute.ps1'
)

foreach ($relativePath in $requiredRelativePaths) {
    $fullPath = [System.IO.Path]::Combine($repoRoot, $relativePath)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2H file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools/runtime/Apply-P102AdminWebRuntimeRunDetailRoute.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = [System.IO.Path]::Combine($repoRoot, $relativeScript)
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw ('Script file is missing: {0}' -f $relativeScript)
    }

    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }

    $forbiddenInvocation = '$' + 'MyInvocation' + '.ScriptName'
    if ($scriptText.IndexOf($forbiddenInvocation, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Avoid fragile invocation-root usage in {0}' -f $relativeScript)
    }
    if ($scriptText.IndexOf('.PackageReference', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $scriptText.IndexOf('.ItemGroup', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $scriptText.IndexOf('.None', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Potential StrictMode-unsafe XML property access in {0}' -f $relativeScript)
    }
}

$pagePath = [System.IO.Path]::Combine($repoRoot, 'src/Admin/Migration.Admin.Web/src/pages/RuntimeRunDetail.tsx')
$pageText = Get-Content -LiteralPath $pagePath -Raw
foreach ($term in @('runtimeDashboardApi.runDetail', 'useParams', 'RuntimeRunDetail', 'workItems')) {
    if ($pageText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('RuntimeRunDetail page is missing expected term: {0}' -f $term)
    }
}

$configPath = [System.IO.Path]::Combine($repoRoot, 'config-samples/p10-admin-web-runtime-run-detail-client.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'runtimeRunDetailRoute')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2H config is missing property: {0}' -f $propertyName)
    }
}
if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2H canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ($config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'P10.2H featureSourcePath must remain apps/migration-admin-ui.'
}

Write-Host 'P10.2H Admin Web runtime run-detail client validation passed.'
