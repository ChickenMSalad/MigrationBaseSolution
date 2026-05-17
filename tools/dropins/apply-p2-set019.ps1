$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set019-queue-worker-loop-diagnostics"

Write-Host "Applying P2 Set 019 from $repoRoot"

$files = @(
    "src\Migration.Admin.Api\Endpoints\QueueWorkerLoopDiagnosticsEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueWorkerLoop.ts",
    "tools\test\smoke-queue-worker-loop-diagnostics.ps1",
    "tools\test\smoke-queue-worker-loop-diagnostics.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_019_QUEUE_WORKER_LOOP_DIAGNOSTICS.md"
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

if ($program -notmatch "MapQueueWorkerLoopDiagnosticsEndpoints") {
    if ($program -match "api\.MapQueueReceiveDiagnosticsEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueReceiveDiagnosticsEndpoints();",
            "api.MapQueueReceiveDiagnosticsEndpoints();`r`napi.MapQueueWorkerLoopDiagnosticsEndpoints();")
        Write-Host "Patched Program.cs queue worker loop diagnostics endpoints."
    }
    elseif ($program -match "api\.MapQueueDispatchDiagnosticsEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueDispatchDiagnosticsEndpoints();",
            "api.MapQueueDispatchDiagnosticsEndpoints();`r`napi.MapQueueWorkerLoopDiagnosticsEndpoints();")
        Write-Host "Patched Program.cs queue worker loop diagnostics endpoints."
    }
    else {
        throw "Could not find queue endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 019 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-worker-loop-diagnostics.ps1 -BaseUrl http://localhost:5173"
