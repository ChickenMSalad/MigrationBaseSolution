$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set047-production-safety-gates"

Write-Host "Applying P2 Set 047 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Operations\ProductionSafetyGateContracts.cs",
    "src\Migration.ControlPlane\Operations\IProductionSafetyGateService.cs",
    "src\Migration.ControlPlane\Operations\ProductionSafetyGateService.cs",
    "src\Migration.ControlPlane\Operations\ProductionSafetyGateRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\ProductionSafetyGateEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\productionSafetyGates.ts",
    "tools\test\smoke-production-safety-gates.ps1",
    "tools\test\smoke-production-safety-gates.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_047_PRODUCTION_SAFETY_GATES.md"
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

if ($program -notmatch "AddProductionSafetyGates") {
    if ($program -match "builder\.Services\.AddAuthEnforcementDiagnostics\(\);") {
        $program = $program.Replace(
            "builder.Services.AddAuthEnforcementDiagnostics();",
            "builder.Services.AddAuthEnforcementDiagnostics();`r`nbuilder.Services.AddProductionSafetyGates();")
        Write-Host "Patched Program.cs production safety gate registration."
    }
    else {
        throw "Could not find auth enforcement diagnostics registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapProductionSafetyGateEndpoints") {
    if ($program -match "api\.MapAuthEnforcementDiagnosticsEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapAuthEnforcementDiagnosticsEndpoints();",
            "api.MapAuthEnforcementDiagnosticsEndpoints();`r`napi.MapProductionSafetyGateEndpoints();")
        Write-Host "Patched Program.cs production safety gate endpoints."
    }
    else {
        throw "Could not find auth enforcement diagnostics endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 047 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-production-safety-gates.ps1 -BaseUrl http://localhost:5173"
