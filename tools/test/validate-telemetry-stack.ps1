param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Telemetry Stack Validation"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$checks = @(
    @{ Name = "telemetry sink"; Script = ".\tools\test\smoke-telemetry-sink.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "telemetry event writer"; Script = ".\tools\test\smoke-telemetry-event-writer.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "queue telemetry events"; Script = ".\tools\test\smoke-queue-telemetry-events.ps1"; Args = @("-BaseUrl", $BaseUrl) },
    @{ Name = "cloud operation telemetry"; Script = ".\tools\test\smoke-cloud-operation-telemetry.ps1"; Args = @("-BaseUrl", $BaseUrl) }
)

foreach ($check in $checks) {
    if (!(Test-Path $check.Script)) {
        throw "Missing validation script for $($check.Name): $($check.Script)"
    }

    Write-Host "Running $($check.Name)..."

    & powershell -ExecutionPolicy Bypass -File $check.Script @($check.Args)

    Write-Host ""
}

Write-Host "Telemetry stack validation completed successfully."
