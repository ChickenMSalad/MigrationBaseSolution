$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set044-endpoint-policy-inventory"

Write-Host "Applying P2 Set 044 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Auth\EndpointPolicyInventoryContracts.cs",
    "src\Migration.ControlPlane\Auth\IEndpointPolicyInventoryService.cs",
    "src\Migration.ControlPlane\Auth\EndpointPolicyInventoryService.cs",
    "src\Migration.ControlPlane\Auth\EndpointPolicyInventoryRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\EndpointPolicyInventoryEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\endpointPolicyInventory.ts",
    "tools\test\smoke-endpoint-policy-inventory.ps1",
    "tools\test\smoke-endpoint-policy-inventory.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_044_ENDPOINT_POLICY_INVENTORY.md"
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

if ($program -notmatch "AddEndpointPolicyInventory") {
    if ($program -match "builder\.Services\.AddAuthPolicyReadiness\(\);") {
        $program = $program.Replace(
            "builder.Services.AddAuthPolicyReadiness();",
            "builder.Services.AddAuthPolicyReadiness();`r`nbuilder.Services.AddEndpointPolicyInventory();")
        Write-Host "Patched Program.cs endpoint policy inventory registration."
    }
    else {
        throw "Could not find auth policy readiness registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapEndpointPolicyInventoryEndpoints") {
    if ($program -match "api\.MapAuthPolicyReadinessEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapAuthPolicyReadinessEndpoints();",
            "api.MapAuthPolicyReadinessEndpoints();`r`napi.MapEndpointPolicyInventoryEndpoints();")
        Write-Host "Patched Program.cs endpoint policy inventory endpoints."
    }
    else {
        throw "Could not find auth policy readiness endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 044 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-endpoint-policy-inventory.ps1 -BaseUrl http://localhost:5173"
