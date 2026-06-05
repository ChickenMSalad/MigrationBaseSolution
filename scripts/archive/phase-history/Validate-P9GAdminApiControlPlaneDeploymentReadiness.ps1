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

function Assert-PathExists {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path missing: $RelativePath"
    }
}

function Assert-FileContains {
    param(
        [string]$RootPath,
        [string]$RelativePath,
        [string]$Text
    )
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file missing: $RelativePath"
    }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p9\P9G-Admin-Api-Control-Plane-Deployment-Readiness.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p9g-admin-api-control-plane-settings.template.json'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Admin.Api'

Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9G-Admin-Api-Control-Plane-Deployment-Readiness.md' -Text 'Do not configure a production RunId override'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9g-admin-api-control-plane-settings.template.json' -Text 'MigrationOperationalStore'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9g-admin-api-control-plane-settings.template.json' -Text 'OpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9g-admin-api-control-plane-settings.template.json' -Text '/health/live'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9g-admin-api-control-plane-settings.template.json' -Text '/health/ready'

Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Admin.Api\Configuration\AdminApiConfigurationExtensions.cs' -Text 'AddEnvironmentVariables(prefix: "MIGRATION_")'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Admin.Api\Contracts\TelemetryCorrelationContracts.cs' -Text 'OpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Admin.Api\Contracts\TelemetryCorrelationContracts.cs' -Text 'ApplicationInsights'

Write-Host 'P9G Admin/API control-plane deployment readiness validation passed.'
