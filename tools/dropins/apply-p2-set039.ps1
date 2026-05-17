$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set039-cloud-operation-telemetry"

Write-Host "Applying P2 Set 039 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Telemetry\CloudOperationTelemetryEventNames.cs",
    "src\Migration.ControlPlane\Telemetry\CloudOperationTelemetryEventFactory.cs",
    "src\Migration.Admin.Api\Endpoints\CloudOperationTelemetryEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\cloudOperationTelemetry.ts",
    "tools\test\smoke-cloud-operation-telemetry.ps1",
    "tools\test\smoke-cloud-operation-telemetry.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_039_CLOUD_OPERATION_TELEMETRY.md"
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

if ($program -notmatch "MapCloudOperationTelemetryEndpoints") {
    if ($program -match "api\.MapQueueTelemetryEventEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapQueueTelemetryEventEndpoints();",
            "api.MapQueueTelemetryEventEndpoints();`r`napi.MapCloudOperationTelemetryEndpoints();")
        Write-Host "Patched Program.cs cloud operation telemetry endpoints."
    }
    elseif ($program -match "api\.MapTelemetryEventWriterEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapTelemetryEventWriterEndpoints();",
            "api.MapTelemetryEventWriterEndpoints();`r`napi.MapCloudOperationTelemetryEndpoints();")
        Write-Host "Patched Program.cs cloud operation telemetry endpoints."
    }
    else {
        throw "Could not find telemetry endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 039 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-cloud-operation-telemetry.ps1 -BaseUrl http://localhost:5173"
