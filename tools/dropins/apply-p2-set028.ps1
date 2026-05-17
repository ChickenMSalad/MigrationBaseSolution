$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set028-queue-execution-readiness"

Write-Host "Applying P2 Set 028 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\QueueExecutionReadinessContracts.cs",
    "src\Migration.ControlPlane\Queues\IQueueExecutionReadinessService.cs",
    "src\Migration.ControlPlane\Queues\QueueExecutionReadinessService.cs",
    "src\Migration.ControlPlane\Queues\QueueExecutionReadinessRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\QueueExecutionReadinessEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueExecutionReadiness.ts",
    "tools\test\smoke-queue-execution-readiness.ps1",
    "tools\test\smoke-queue-execution-readiness.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_028_QUEUE_EXECUTION_READINESS.md"
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

if ($program -notmatch "AddQueueExecutionReadiness") {
    if ($program -match "builder\.Services\.AddQueueExecutionObservability\(\);") {
        $program = $program.Replace(
            "builder.Services.AddQueueExecutionObservability();",
            "builder.Services.AddQueueExecutionObservability();`r`nbuilder.Services.AddQueueExecutionReadiness();")
        Write-Host "Patched Program.cs queue execution readiness registration."
    }
    else {
        throw "Could not find queue execution observability registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapQueueExecutionReadinessEndpoints") {
    if ($program -match "api\.MapQueueExecutionObservabilityEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueExecutionObservabilityEndpoints();",
            "api.MapQueueExecutionObservabilityEndpoints();`r`napi.MapQueueExecutionReadinessEndpoints();")
        Write-Host "Patched Program.cs queue execution readiness endpoints."
    }
    else {
        throw "Could not find queue execution observability endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 028 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-execution-readiness.ps1 -BaseUrl http://localhost:5173"
