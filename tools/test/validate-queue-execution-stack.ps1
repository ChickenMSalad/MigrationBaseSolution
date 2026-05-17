param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$AllowUnconfigured
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Execution Stack Validation"
Write-Host "Base URL          : $BaseUrl"
Write-Host "AllowUnconfigured: $AllowUnconfigured"
Write-Host ""

$checks = @(
    @{ Name = "queue contracts"; Script = ".\tools\test\smoke-queue-contracts.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "queue idempotency"; Script = ".\tools\test\smoke-queue-idempotency.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "queue dispatch"; Script = ".\tools\test\smoke-queue-dispatch.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "azure queue dispatch"; Script = ".\tools\test\smoke-azure-queue-dispatch.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "queue receive"; Script = ".\tools\test\smoke-queue-receive.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "worker loop diagnostics"; Script = ".\tools\test\smoke-queue-worker-loop-diagnostics.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "poison handling"; Script = ".\tools\test\smoke-queue-poison-handling.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "failure artifact"; Script = ".\tools\test\smoke-queue-failure-artifact.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "failure handler"; Script = ".\tools\test\smoke-queue-failure-handler.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "execution planner"; Script = ".\tools\test\smoke-queue-execution-planner.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "executor coordinator"; Script = ".\tools\test\smoke-queue-executor-coordinator.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "execution observability"; Script = ".\tools\test\smoke-queue-execution-observability.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "execution readiness"; Script = ".\tools\test\smoke-queue-execution-readiness.ps1"; Args = @("-BaseUrl", $BaseUrl) }
)

foreach ($check in $checks) {
    if (!(Test-Path $check.Script)) {
        throw "Missing validation script for $($check.Name): $($check.Script)"
    }

    Write-Host "Running $($check.Name)..."

    $args = @($check.Args)

    if ($AllowUnconfigured -and (
        $check.Script -like "*smoke-queue-dispatch.ps1" -or
        $check.Script -like "*smoke-azure-queue-dispatch.ps1" -or
        $check.Script -like "*smoke-queue-receive.ps1")) {
        $args += "-AllowUnconfigured"
    }

    & powershell -ExecutionPolicy Bypass -File $check.Script @args

    Write-Host ""
}

Write-Host "Queue execution stack validation completed successfully."
