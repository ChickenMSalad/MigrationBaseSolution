$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set023-queue-execution-planner"

Write-Host "Applying P2 Set 023 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\QueueExecutionPlanningContracts.cs",
    "src\Migration.ControlPlane\Queues\IQueueExecutionPlanner.cs",
    "src\Migration.ControlPlane\Queues\QueueExecutionPlanner.cs",
    "src\Migration.ControlPlane\Queues\QueueExecutionPlannerRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\QueueExecutionPlannerEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueExecutionPlanner.ts",
    "tools\test\smoke-queue-execution-planner.ps1",
    "tools\test\smoke-queue-execution-planner.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_023_QUEUE_EXECUTION_PLANNER.md"
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

if ($program -notmatch "AddQueueExecutionPlanning") {
    if ($program -match "builder\.Services\.AddQueueFailureHandling\(\);") {
        $program = $program.Replace(
            "builder.Services.AddQueueFailureHandling();",
            "builder.Services.AddQueueFailureHandling();`r`nbuilder.Services.AddQueueExecutionPlanning();")
        Write-Host "Patched Program.cs queue execution planning registration."
    }
    else {
        throw "Could not find queue failure handling registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapQueueExecutionPlannerEndpoints") {
    if ($program -match "api\.MapQueueFailureHandlerEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueFailureHandlerEndpoints();",
            "api.MapQueueFailureHandlerEndpoints();`r`napi.MapQueueExecutionPlannerEndpoints();")
        Write-Host "Patched Program.cs queue execution planner endpoints."
    }
    else {
        throw "Could not find queue failure handler endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 023 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-execution-planner.ps1 -BaseUrl http://localhost:5173"
