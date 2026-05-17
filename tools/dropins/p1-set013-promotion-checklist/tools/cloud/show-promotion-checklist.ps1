param(
    [string]$Environment = "dev"
)

$ErrorActionPreference = "Stop"

$allowed = @("local-dev", "dev", "test", "prod")
if ($allowed -notcontains $Environment) {
    throw "Environment must be one of: $($allowed -join ', ')"
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$checklistPath = Join-Path $repoRoot "docs\azure\AZURE_ENVIRONMENT_PROMOTION_CHECKLIST.md"

if (!(Test-Path $checklistPath)) {
    throw "Checklist not found: $checklistPath"
}

Write-Host "Promotion checklist: $Environment"
Write-Host "File: $checklistPath"
Write-Host ""
Write-Host "Recommended validation commands:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "  cd .\src\Admin\Migration.Admin.Web; npm run build"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -BaseUrl http://localhost:5173"
Write-Host ""
Write-Host "Open checklist:"
Write-Host "  $checklistPath"
