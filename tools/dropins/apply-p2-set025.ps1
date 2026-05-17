$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set025-worker-coordinator-registration-plan"

Write-Host "Applying P2 Set 025 from $repoRoot"

$files = @(
    "src\Workers\Migration.Workers.QueueExecutor\QueueExecutorWorkerRegistrationPlan.cs",
    "src\Workers\Migration.Workers.QueueExecutor\QUEUE_EXECUTOR_WORKER_REGISTRATION.md",
    "tools\test\smoke-worker-coordinator-registration-plan.ps1",
    "tools\test\smoke-worker-coordinator-registration-plan.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_025_WORKER_COORDINATOR_REGISTRATION_PLAN.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Missing file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

Write-Host ""
Write-Host "P2 Set 025 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Then validate:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-worker-coordinator-registration-plan.ps1"
