$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set018-queue-worker-loop"

Write-Host "Applying P2 Set 018 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\QueueWorkerLoopOptions.cs",
    "src\Migration.ControlPlane\Queues\QueueWorkerLoopDescriptor.cs",
    "src\Migration.ControlPlane\Queues\QueueWorkerLoopPlanner.cs",
    "src\Workers\Migration.Workers.QueueExecutor\QueueWorkerLoopService.cs",
    "tools\test\smoke-queue-worker-loop.ps1",
    "tools\test\smoke-queue-worker-loop.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_018_QUEUE_WORKER_LOOP_SCAFFOLD.md"
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

$programCandidates = @(
    "src\Workers\Migration.Workers.QueueExecutor\Program.cs",
    "src\Workers\Migration.Workers.QueueExecutor\WorkerProgram.cs"
) | ForEach-Object { Join-Path $repoRoot $_ }

$programPath = $programCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($null -ne $programPath) {
    $program = Get-Content $programPath -Raw

    if ($program -notmatch "AddQueueReceiveProvider") {
        if ($program -match "services\.Add") {
            # Do not attempt risky patching against unknown worker bootstrap.
            Write-Host "Worker startup exists but was not auto-patched. Register AddQueueReceiveProvider and QueueWorkerLoopService manually if desired."
        }
    }

    if ($program -notmatch "QueueWorkerLoopService") {
        Write-Host "Worker loop service file was added but not auto-registered to avoid unsafe startup patching."
    }
}
else {
    Write-Host "No worker Program.cs found to patch; added service scaffold only."
}

Write-Host ""
Write-Host "P2 Set 018 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Validate:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-worker-loop.ps1"
