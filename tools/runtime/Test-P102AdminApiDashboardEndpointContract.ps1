[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot
)

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

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
}

$programPath = Join-Path $RepoRoot 'src\Core\Migration.Admin.Api\Program.cs'
$dashboardPath = Join-Path $RepoRoot 'src\Core\Migration.Admin.Api\Endpoints\Operational\Dashboard\RuntimeDashboardEndpointExtensions.cs'
$projectPath = Join-Path $RepoRoot 'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj'

foreach ($path in @($programPath, $dashboardPath, $projectPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('Required Admin API file is missing: {0}' -f $path)
    }
}

$programText = Get-Content -LiteralPath $programPath -Raw
$dashboardText = Get-Content -LiteralPath $dashboardPath -Raw

$requiredProgramTerms = @(
    'using Migration.Admin.Api.Endpoints.Operational.Dashboard;',
    'app.MapSqlOperationalRuntimeDashboardEndpoints();'
)

foreach ($term in $requiredProgramTerms) {
    if ($programText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Program.cs is missing expected dashboard wiring term: {0}' -f $term)
    }
}

$requiredDashboardTerms = @(
    'MapSqlOperationalRuntimeDashboardEndpoints',
    '.MapGroup("/api/runtime/dashboard")',
    'group.MapGet("/summary", GetSummaryAsync)',
    'group.MapGet("/runs", GetRunsAsync)',
    'migration.Runs',
    'migration.WorkItems',
    'MigrationOperationalStore',
    'SqlOperationalRuntimeReadiness:ConnectionString'
)

foreach ($term in $requiredDashboardTerms) {
    if ($dashboardText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Runtime dashboard endpoint file is missing expected term: {0}' -f $term)
    }
}

Write-Host 'P10.2B Admin API dashboard endpoint contract validation passed.'
