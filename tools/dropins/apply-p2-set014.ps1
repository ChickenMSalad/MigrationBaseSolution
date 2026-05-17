$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set014-queue-idempotency"

Write-Host "Applying P2 Set 014 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Queues\QueueMessageSerialization.cs",
    "src\Migration.ControlPlane\Queues\QueueIdempotencyKeyBuilder.cs",
    "src\Migration.Admin.Api\Endpoints\QueueIdempotencyEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueIdempotency.ts",
    "tools\test\smoke-queue-idempotency.ps1",
    "tools\test\smoke-queue-idempotency.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_014_QUEUE_SERIALIZATION_IDEMPOTENCY.md"
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

if ($program -notmatch "MapQueueIdempotencyEndpoints") {
    if ($program -match "api\.MapQueueContractDiagnosticsEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueContractDiagnosticsEndpoints();",
            "api.MapQueueContractDiagnosticsEndpoints();`r`napi.MapQueueIdempotencyEndpoints();")
    }
    elseif ($program -match "api\.MapQueueProviderPlanEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueProviderPlanEndpoints();",
            "api.MapQueueProviderPlanEndpoints();`r`napi.MapQueueIdempotencyEndpoints();")
    }
    else {
        throw "Could not find queue endpoint mapping anchor in Program.cs."
    }

    Set-Content -Path $programPath -Value $program -Encoding UTF8
    Write-Host "Patched Program.cs queue idempotency endpoints."
}

Write-Host ""
Write-Host "P2 Set 014 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-idempotency.ps1 -BaseUrl http://localhost:5173"
