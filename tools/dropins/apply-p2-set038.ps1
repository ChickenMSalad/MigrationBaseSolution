$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set038-queue-telemetry-events"

Write-Host "Applying P2 Set 038 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Telemetry\QueueTelemetryEventNames.cs",
    "src\Migration.ControlPlane\Telemetry\QueueTelemetryEventFactory.cs",
    "src\Migration.Admin.Api\Endpoints\QueueTelemetryEventEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\queueTelemetryEvents.ts",
    "tools\test\smoke-queue-telemetry-events.ps1",
    "tools\test\smoke-queue-telemetry-events.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_038_QUEUE_TELEMETRY_EVENTS.md"
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

if ($program -notmatch "MapQueueTelemetryEventEndpoints") {
    if ($program -match "api\.MapTelemetryEventWriterEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapTelemetryEventWriterEndpoints();",
            "api.MapTelemetryEventWriterEndpoints();`r`napi.MapQueueTelemetryEventEndpoints();")
        Write-Host "Patched Program.cs queue telemetry event endpoints."
    }
    elseif ($program -match "api\.MapTelemetrySinkEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapTelemetrySinkEndpoints();",
            "api.MapTelemetrySinkEndpoints();`r`napi.MapQueueTelemetryEventEndpoints();")
        Write-Host "Patched Program.cs queue telemetry event endpoints."
    }
    else {
        throw "Could not find telemetry endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 038 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-telemetry-events.ps1 -BaseUrl http://localhost:5173"
