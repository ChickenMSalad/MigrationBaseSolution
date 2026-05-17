param(
    [string]$ProjectPath = ".\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
)

$ErrorActionPreference = "Stop"

Write-Host "Worker Coordinator Registration Plan Smoke Test"
Write-Host "Project: $ProjectPath"
Write-Host ""

if (!(Test-Path $ProjectPath)) {
    throw "Worker project not found: $ProjectPath"
}

$planFile = ".\src\Workers\Migration.Workers.QueueExecutor\QueueExecutorWorkerRegistrationPlan.cs"
$docFile = ".\src\Workers\Migration.Workers.QueueExecutor\QUEUE_EXECUTOR_WORKER_REGISTRATION.md"

if (!(Test-Path $planFile)) {
    throw "Expected plan file not found: $planFile"
}

if (!(Test-Path $docFile)) {
    throw "Expected registration documentation not found: $docFile"
}

dotnet build $ProjectPath

Write-Host ""
Write-Host "Worker coordinator registration plan smoke test completed successfully."
