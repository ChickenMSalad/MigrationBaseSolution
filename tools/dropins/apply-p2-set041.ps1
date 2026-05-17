$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set041-operational-readiness-rollups"

Write-Host "Applying P2 Set 041 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Operations\OperationalReadinessContracts.cs",
    "src\Migration.ControlPlane\Operations\IOperationalReadinessService.cs",
    "src\Migration.ControlPlane\Operations\OperationalReadinessService.cs",
    "src\Migration.ControlPlane\Operations\OperationalReadinessRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\OperationalReadinessEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\operationalReadiness.ts",
    "tools\test\smoke-operational-readiness-rollups.ps1",
    "tools\test\smoke-operational-readiness-rollups.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_041_OPERATIONAL_READINESS_ROLLUPS.md"
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

if ($program -notmatch "AddOperationalReadiness") {
    if ($program -match "builder\.Services\.AddTelemetryEventWriter\(\);") {
        $program = $program.Replace(
            "builder.Services.AddTelemetryEventWriter();",
            "builder.Services.AddTelemetryEventWriter();`r`nbuilder.Services.AddOperationalReadiness();")
        Write-Host "Patched Program.cs operational readiness registration."
    }
    else {
        throw "Could not find telemetry event writer registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapOperationalReadinessEndpoints") {
    if ($program -match "api\.MapCloudOperationTelemetryEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapCloudOperationTelemetryEndpoints();",
            "api.MapCloudOperationTelemetryEndpoints();`r`napi.MapOperationalReadinessEndpoints();")
        Write-Host "Patched Program.cs operational readiness endpoints."
    }
    elseif ($program -match "api\.MapTelemetrySinkEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapTelemetrySinkEndpoints();",
            "api.MapTelemetrySinkEndpoints();`r`napi.MapOperationalReadinessEndpoints();")
        Write-Host "Patched Program.cs operational readiness endpoints."
    }
    else {
        throw "Could not find telemetry endpoint mapping anchor in Program.cs."
    }
}

if ($program -notmatch "using Migration\.ControlPlane\.Operations;") {
    $program = "using Migration.ControlPlane.Operations;`r`n" + $program
    Write-Host "Patched Program.cs using Migration.ControlPlane.Operations;"
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 041 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-operational-readiness-rollups.ps1 -BaseUrl http://localhost:5173"
