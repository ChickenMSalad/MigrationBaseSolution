$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set020-queue-poison-planning"

Write-Host "Applying P2 Set 020 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\QueuePoisonMessageContracts.cs",
    "src\Migration.ControlPlane\Queues\QueuePoisonHandlingPlanner.cs",
    "src\Migration.Admin.Api\Endpoints\QueuePoisonHandlingEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queuePoisonHandling.ts",
    "tools\test\smoke-queue-poison-handling.ps1",
    "tools\test\smoke-queue-poison-handling.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_020_QUEUE_POISON_HANDLING.md"
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

if ($program -notmatch "MapQueuePoisonHandlingEndpoints") {
    if ($program -match "api\.MapQueueWorkerLoopDiagnosticsEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueWorkerLoopDiagnosticsEndpoints();",
            "api.MapQueueWorkerLoopDiagnosticsEndpoints();`r`napi.MapQueuePoisonHandlingEndpoints();")
        Write-Host "Patched Program.cs queue poison handling endpoints."
    }
    elseif ($program -match "api\.MapQueueReceiveDiagnosticsEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueReceiveDiagnosticsEndpoints();",
            "api.MapQueueReceiveDiagnosticsEndpoints();`r`napi.MapQueuePoisonHandlingEndpoints();")
        Write-Host "Patched Program.cs queue poison handling endpoints."
    }
    else {
        throw "Could not find queue endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 020 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-poison-handling.ps1 -BaseUrl http://localhost:5173"
