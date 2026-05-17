$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set037-telemetry-event-writer"

Write-Host "Applying P2 Set 037 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Telemetry\ITelemetryEventWriter.cs",
    "src\Migration.ControlPlane\Telemetry\TelemetryEventWriter.cs",
    "src\Migration.ControlPlane\Telemetry\TelemetryEventWriterRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\TelemetryEventWriterEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\telemetryEventWriter.ts",
    "tools\test\smoke-telemetry-event-writer.ps1",
    "tools\test\smoke-telemetry-event-writer.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_037_TELEMETRY_EVENT_WRITER.md"
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

if ($program -notmatch "AddTelemetryEventWriter") {
    if ($program -match "builder\.Services\.AddTelemetrySink\(builder\.Configuration\);") {
        $program = $program.Replace(
            "builder.Services.AddTelemetrySink(builder.Configuration);",
            "builder.Services.AddTelemetrySink(builder.Configuration);`r`nbuilder.Services.AddTelemetryEventWriter();")
        Write-Host "Patched Program.cs telemetry event writer registration."
    }
    else {
        throw "Could not find telemetry sink registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapTelemetryEventWriterEndpoints") {
    if ($program -match "api\.MapTelemetrySinkEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapTelemetrySinkEndpoints();",
            "api.MapTelemetrySinkEndpoints();`r`napi.MapTelemetryEventWriterEndpoints();")
        Write-Host "Patched Program.cs telemetry event writer endpoints."
    }
    else {
        throw "Could not find telemetry sink endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 037 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-telemetry-event-writer.ps1 -BaseUrl http://localhost:5173"
