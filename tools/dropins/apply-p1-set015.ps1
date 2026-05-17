$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set015-telemetry-correlation"

Write-Host "Applying P1 Set 015 from $repoRoot"

$files = @(
    "src\Migration.Admin.Api\Contracts\TelemetryCorrelationContracts.cs",
    "src\Migration.Admin.Api\Endpoints\TelemetryCorrelationEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\telemetryCorrelation.ts",
    "docs\cloud-roadmap-cleanup\P1_SET_015_TELEMETRY_CORRELATION.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Drop-in package is missing expected file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "MapTelemetryCorrelationEndpoints") {
    if ($program -match "api\.MapCloudPlatformEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudPlatformEndpoints\(\);", "api.MapCloudPlatformEndpoints();`r`napi.MapTelemetryCorrelationEndpoints();"
        Set-Content -Path $programPath -Value $program -Encoding UTF8
        Write-Host "Patched Program.cs after api.MapCloudPlatformEndpoints();"
    }
    elseif ($program -match "api\.MapCloudReadinessEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudReadinessEndpoints\(\);", "api.MapCloudReadinessEndpoints();`r`napi.MapTelemetryCorrelationEndpoints();"
        Set-Content -Path $programPath -Value $program -Encoding UTF8
        Write-Host "Patched Program.cs after api.MapCloudReadinessEndpoints();"
    }
    else {
        throw "Could not find cloud endpoint mapping anchor in Program.cs. No partial patch was written."
    }
}
else {
    Write-Host "Program.cs already maps telemetry correlation endpoints."
}

Write-Host ""
Write-Host "P1 Set 015 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Then verify:"
Write-Host "  Invoke-RestMethod http://localhost:5173/api/cloud/telemetry/correlation"
