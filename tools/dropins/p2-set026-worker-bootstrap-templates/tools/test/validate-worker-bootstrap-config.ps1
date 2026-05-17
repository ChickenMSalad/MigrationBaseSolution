param(
    [string]$ConfigPath = ".\config\worker\queue-executor.dryrun.appsettings.example.json"
)

$ErrorActionPreference = "Stop"

Write-Host "Worker Bootstrap Config Validation"
Write-Host "Config: $ConfigPath"
Write-Host ""

if (!(Test-Path $ConfigPath)) {
    throw "Config file not found: $ConfigPath"
}

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json

if ($null -eq $config.MigrationRunQueue) {
    throw "Missing MigrationRunQueue section."
}

if ($null -eq $config.QueueWorkerLoop) {
    throw "Missing QueueWorkerLoop section."
}

if ($null -eq $config.QueueExecutorCoordinator) {
    throw "Missing QueueExecutorCoordinator section."
}

if ($config.QueueWorkerLoop.Enabled -eq $true) {
    throw "Safety violation: QueueWorkerLoop.Enabled must be false in example templates."
}

if ($config.QueueWorkerLoop.DryRun -ne $true) {
    throw "Safety violation: QueueWorkerLoop.DryRun must be true in example templates."
}

if ($config.QueueExecutorCoordinator.DryRun -ne $true) {
    throw "Safety violation: QueueExecutorCoordinator.DryRun must be true in example templates."
}

if ($config.QueueExecutorCoordinator.CompleteMessages -eq $true) {
    throw "Safety violation: QueueExecutorCoordinator.CompleteMessages must be false in example templates."
}

Write-Host "Provider          : $($config.MigrationRunQueue.Provider)"
Write-Host "Queue             : $($config.MigrationRunQueue.QueueName)"
Write-Host "Worker enabled    : $($config.QueueWorkerLoop.Enabled)"
Write-Host "Worker dry-run    : $($config.QueueWorkerLoop.DryRun)"
Write-Host "Coordinator dryrun: $($config.QueueExecutorCoordinator.DryRun)"
Write-Host ""
Write-Host "Worker bootstrap config validation completed successfully."
