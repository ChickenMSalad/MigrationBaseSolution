$ErrorActionPreference = "Stop"

Write-Host "Worker Bootstrap Templates Smoke Test"
Write-Host ""

$templates = @(
    ".\config\worker\queue-executor.dryrun.appsettings.example.json",
    ".\config\worker\queue-executor.local-inmemory.appsettings.example.json",
    ".\config\worker\queue-executor.azurequeue.appsettings.example.json"
)

foreach ($template in $templates) {
    powershell -ExecutionPolicy Bypass -File .\tools\test\validate-worker-bootstrap-config.ps1 -ConfigPath $template
    Write-Host ""
}

dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj

Write-Host ""
Write-Host "Worker bootstrap templates smoke test completed successfully."
