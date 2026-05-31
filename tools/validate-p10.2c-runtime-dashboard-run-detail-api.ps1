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
    'docs\p10\P10.2C-Runtime-Dashboard-Run-Detail-Api.md',
    'config-samples\p10-runtime-dashboard-run-detail-api.sample.json',
    'src\Core\Migration.Admin.Api\Endpoints\Operational\Dashboard\RuntimeDashboardDetailEndpointExtensions.cs',
    'tools\runtime\Apply-P102RuntimeDashboardRunDetailEndpoints.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.2C file is missing: {0}' -f $relativePath)
    }
}

$scripts = @(
    'tools\runtime\Apply-P102RuntimeDashboardRunDetailEndpoints.ps1'
)

foreach ($relativeScript in $scripts) {
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

$endpointPath = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Endpoints\Operational\Dashboard\RuntimeDashboardDetailEndpointExtensions.cs'
$endpointText = Get-Content -LiteralPath $endpointPath -Raw
foreach ($requiredTerm in @(
    'MapSqlOperationalRuntimeDashboardDetailEndpoints',
    '/runs/{runId:guid}',
    '/runs/{runId:guid}/work-items',
    '/runs/{runId:guid}/failures',
    'migration.Runs',
    'migration.WorkItems',
    'MigrationOperationalStore'
)) {
    if ($endpointText.IndexOf($requiredTerm, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Runtime dashboard detail endpoint file is missing expected term: {0}' -f $requiredTerm)
    }
}

$programPath = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Program.cs'
if (Test-Path -LiteralPath $programPath) {
    $programText = Get-Content -LiteralPath $programPath -Raw
    if ($programText.IndexOf('app.MapSqlOperationalRuntimeDashboardEndpoints();', [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Admin API Program.cs is missing the base runtime dashboard endpoint mapping.'
    }
}

Write-Host 'P10.2C runtime dashboard run-detail API validation passed.'
