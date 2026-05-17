$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set048-operational-mode-state"

Write-Host "Applying P2 Set 048 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Operations\OperationalModeContracts.cs",
    "src\Migration.ControlPlane\Operations\IOperationalModeService.cs",
    "src\Migration.ControlPlane\Operations\OperationalModeService.cs",
    "src\Migration.ControlPlane\Operations\OperationalModeRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\OperationalModeEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\operationalMode.ts",
    "tools\test\smoke-operational-mode.ps1",
    "tools\test\smoke-operational-mode.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_048_OPERATIONAL_MODE_STATE.md"
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

if ($program -notmatch "AddOperationalMode") {
    if ($program -match "builder\.Services\.AddProductionSafetyGates\(\);") {
        $program = $program.Replace(
            "builder.Services.AddProductionSafetyGates();",
            "builder.Services.AddProductionSafetyGates();`r`nbuilder.Services.AddOperationalMode();")
        Write-Host "Patched Program.cs operational mode registration."
    }
    else {
        throw "Could not find production safety gate registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapOperationalModeEndpoints") {
    if ($program -match "api\.MapProductionSafetyGateEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapProductionSafetyGateEndpoints();",
            "api.MapProductionSafetyGateEndpoints();`r`napi.MapOperationalModeEndpoints();")
        Write-Host "Patched Program.cs operational mode endpoints."
    }
    else {
        throw "Could not find production safety gate endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 048 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-operational-mode.ps1 -BaseUrl http://localhost:5173"
