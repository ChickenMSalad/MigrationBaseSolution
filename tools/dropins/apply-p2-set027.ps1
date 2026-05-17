$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set027-queue-execution-observability"

Write-Host "Applying P2 Set 027 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\QueueExecutionObservabilityContracts.cs",
    "src\Migration.ControlPlane\Queues\IQueueExecutionObservabilityService.cs",
    "src\Migration.ControlPlane\Queues\QueueExecutionObservabilityService.cs",
    "src\Migration.ControlPlane\Queues\QueueExecutionObservabilityRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\QueueExecutionObservabilityEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueExecutionObservability.ts",
    "tools\test\smoke-queue-execution-observability.ps1",
    "tools\test\smoke-queue-execution-observability.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_027_QUEUE_EXECUTION_OBSERVABILITY.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "AddQueueExecutionObservability") {
    $program = $program.Replace(
        "builder.Services.AddQueueExecutorCoordinator(builder.Configuration);",
        "builder.Services.AddQueueExecutorCoordinator(builder.Configuration);`r`nbuilder.Services.AddQueueExecutionObservability();")
}

if ($program -notmatch "MapQueueExecutionObservabilityEndpoints") {
    $program = $program.Replace(
        "api.MapQueueExecutorCoordinatorEndpoints();",
        "api.MapQueueExecutorCoordinatorEndpoints();`r`napi.MapQueueExecutionObservabilityEndpoints();")
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 027 applied."
