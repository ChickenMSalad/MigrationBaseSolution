$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set024-queue-executor-coordinator"

Write-Host "Applying P2 Set 024 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\QueueExecutorCoordinatorContracts.cs",
    "src\Migration.ControlPlane\Queues\IQueueExecutorCoordinator.cs",
    "src\Migration.ControlPlane\Queues\QueueExecutorCoordinator.cs",
    "src\Migration.ControlPlane\Queues\QueueExecutorCoordinatorRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\QueueExecutorCoordinatorEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueExecutorCoordinator.ts",
    "tools\test\smoke-queue-executor-coordinator.ps1",
    "tools\test\smoke-queue-executor-coordinator.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_024_QUEUE_EXECUTOR_COORDINATOR.md"
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

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "AddQueueExecutorCoordinator") {
    if ($program -match "builder\.Services\.AddQueueExecutionPlanning\(\);") {
        $program = $program.Replace(
            "builder.Services.AddQueueExecutionPlanning();",
            "builder.Services.AddQueueExecutionPlanning();`r`nbuilder.Services.AddQueueExecutorCoordinator(builder.Configuration);")
        Write-Host "Patched Program.cs queue executor coordinator registration."
    }
    else {
        throw "Could not find queue execution planning registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapQueueExecutorCoordinatorEndpoints") {
    if ($program -match "api\.MapQueueExecutionPlannerEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueExecutionPlannerEndpoints();",
            "api.MapQueueExecutionPlannerEndpoints();`r`napi.MapQueueExecutorCoordinatorEndpoints();")
        Write-Host "Patched Program.cs queue executor coordinator endpoints."
    }
    else {
        throw "Could not find queue execution planner endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 024 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-executor-coordinator.ps1 -BaseUrl http://localhost:5173"
