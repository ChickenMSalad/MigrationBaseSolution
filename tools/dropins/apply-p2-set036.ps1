$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set036-telemetry-provider-contracts"

Write-Host "Applying P2 Set 036 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Telemetry\TelemetryContracts.cs",
    "src\Migration.ControlPlane\Telemetry\ITelemetrySink.cs",
    "src\Migration.ControlPlane\Telemetry\InMemoryTelemetrySink.cs",
    "src\Migration.ControlPlane\Telemetry\TelemetryEventFactory.cs",
    "src\Migration.ControlPlane\Telemetry\TelemetryRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\TelemetrySinkEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\telemetrySink.ts",
    "tools\test\smoke-telemetry-sink.ps1",
    "tools\test\smoke-telemetry-sink.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_036_TELEMETRY_PROVIDER_CONTRACTS.md"
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

if ($program -notmatch "AddTelemetrySink") {
    if ($program -match "builder\.Services\.AddAuditEventWriter\(\);") {
        $program = $program.Replace(
            "builder.Services.AddAuditEventWriter();",
            "builder.Services.AddAuditEventWriter();`r`nbuilder.Services.AddTelemetrySink(builder.Configuration);")
        Write-Host "Patched Program.cs telemetry sink registration."
    }
    else {
        throw "Could not find audit event writer registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapTelemetrySinkEndpoints") {
    if ($program -match "api\.MapTelemetryCorrelationEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapTelemetryCorrelationEndpoints();",
            "api.MapTelemetryCorrelationEndpoints();`r`napi.MapTelemetrySinkEndpoints();")
        Write-Host "Patched Program.cs telemetry sink endpoints."
    }
    else {
        throw "Could not find telemetry endpoint mapping anchor in Program.cs."
    }
}

if ($program -notmatch "using Migration\.ControlPlane\.Telemetry;") {
    $program = "using Migration.ControlPlane.Telemetry;`r`n" + $program
    Write-Host "Patched Program.cs using Migration.ControlPlane.Telemetry;"
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 036 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-telemetry-sink.ps1 -BaseUrl http://localhost:5173"
