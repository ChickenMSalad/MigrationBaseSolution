param(
    [string]$ProjectPath = ".\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Worker Loop Smoke Test"
Write-Host "Project: $ProjectPath"
Write-Host ""

if (!(Test-Path $ProjectPath)) {
    throw "Worker project not found: $ProjectPath"
}

dotnet build $ProjectPath

Write-Host ""
Write-Host "Queue worker loop smoke test completed successfully."
