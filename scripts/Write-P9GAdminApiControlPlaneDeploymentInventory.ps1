Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Add-Line {
    param([string]$Text)
    [void]$script:Lines.Add($Text)
}

function Add-FileSummary {
    param(
        [string]$Title,
        [string]$RelativePath,
        [string[]]$Patterns
    )
    Add-Line ''
    Add-Line "## $RelativePath"
    Add-Line ''
    $path = Join-Path $script:Root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line 'Missing.'
        return
    }
    Add-Line 'Present.'
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            Add-Line (('- Contains: {0}' -f $pattern))
        }
        else {
            Add-Line (('- Missing: {0}' -f $pattern))
        }
    }
}

$script:Root = Get-RepositoryRoot
$script:Lines = New-Object System.Collections.Generic.List[string]

Add-Line '# P9G Admin/API Control-Plane Deployment Inventory'
Add-Line ''
Add-Line ('GeneratedUtc: {0:o}' -f [DateTimeOffset]::UtcNow)
Add-Line ''
Add-Line 'This inventory verifies repository-side Admin/API control-plane deployment readiness before cloud deployment proof.'

Add-FileSummary -Title 'Doc' -RelativePath 'docs\p9\P9G-Admin-Api-Control-Plane-Deployment-Readiness.md' -Patterns @(
    'Admin API',
    'health',
    'readiness',
    'Do not configure a production RunId override'
)

Add-FileSummary -Title 'Template' -RelativePath 'config\templates\p9g-admin-api-control-plane-settings.template.json' -Patterns @(
    'MigrationOperationalStore',
    'OpenTelemetry',
    'AzureMonitorConnectionString',
    '/health/live',
    '/health/ready'
)

Add-FileSummary -Title 'Admin config' -RelativePath 'src\Core\Migration.Admin.Api\Configuration\AdminApiConfigurationExtensions.cs' -Patterns @(
    'AddEnvironmentVariables(prefix: "MIGRATION_")'
)

Add-FileSummary -Title 'Telemetry contracts' -RelativePath 'src\Core\Migration.Admin.Api\Contracts\TelemetryCorrelationContracts.cs' -Patterns @(
    'OpenTelemetry',
    'ApplicationInsights',
    'X-Correlation-Id'
)

Add-FileSummary -Title 'Telemetry endpoint' -RelativePath 'src\Core\Migration.Admin.Api\Endpoints\Telemetry\TelemetryCorrelationEndpointExtensions.cs' -Patterns @(
    'ApplicationInsights:ConnectionString',
    'APPLICATIONINSIGHTS_CONNECTION_STRING',
    'OpenTelemetry'
)

Add-Line ''
Add-Line '## Recommended next checks'
Add-Line ''
Add-Line '- Deploy the Admin/API control plane after SQL and Service Bus validation.'
Add-Line '- Verify /health/live and /health/ready.'
Add-Line '- Verify telemetry/correlation endpoint output.'
Add-Line '- Confirm no production RunId override is configured.'

$out = Join-Path $script:Root 'docs\p9\P9G-Admin-Api-Control-Plane-Deployment-Inventory.generated.md'
$dir = Split-Path -Parent $out
if (-not (Test-Path -LiteralPath $dir)) {
    New-Item -ItemType Directory -Path $dir | Out-Null
}
Set-Content -LiteralPath $out -Value $script:Lines -Encoding UTF8
Write-Host "Wrote $out"
