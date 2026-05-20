$ErrorActionPreference = "Stop"

$files = @(
    "src\Migration.Admin.Api\Endpoints\Operational\Diagnostics\OperationalMirrorDiagnosticsEndpointExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\Operational\Diagnostics\OperationalSqlSchemaDiagnosticsEndpointExtensions.cs",
    "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
)

foreach ($file in $files) {
    Write-Host ""
    Write-Host "=============================="
    Write-Host $file
    Write-Host "=============================="
    Get-Content $file
}
