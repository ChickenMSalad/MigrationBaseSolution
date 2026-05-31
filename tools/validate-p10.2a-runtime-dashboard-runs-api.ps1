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
    'docs\p10\P10.2A-Runtime-Dashboard-Runs-Api.md',
    'config-samples\p10-runtime-dashboard-runs-api.sample.json',
    'src\Core\Migration.Admin.Api\Endpoints\Operational\Dashboard\RuntimeDashboardEndpointExtensions.cs',
    'tools\runtime\Apply-P102RuntimeDashboardEndpoints.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.2A file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Apply-P102RuntimeDashboardEndpoints.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }

    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    $fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
    if ($scriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Avoid fragile invocation-root usage in {0}' -f $relativeScript)
    }
    if ($scriptText.IndexOf('.PackageReference', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $scriptText.IndexOf('.None', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $scriptText.IndexOf('.ItemGroup', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Potential StrictMode-unsafe XML property access in {0}' -f $relativeScript)
    }
}

$endpointPath = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Endpoints\Operational\Dashboard\RuntimeDashboardEndpointExtensions.cs'
$endpointText = Get-Content -LiteralPath $endpointPath -Raw
foreach ($term in @('/api/runtime/dashboard', 'migration.Runs', 'migration.WorkItems', 'MapSqlOperationalRuntimeDashboardEndpoints')) {
    if ($endpointText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Runtime dashboard endpoint is missing expected term: {0}' -f $term)
    }
}

Write-Host 'P10.2A runtime dashboard runs API validation passed.'
