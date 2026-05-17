$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set045-credential-access-policy-readiness"

Write-Host "Applying P2 Set 045 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Auth\CredentialAccessPolicyContracts.cs",
    "src\Migration.ControlPlane\Auth\ICredentialAccessPolicyReadinessService.cs",
    "src\Migration.ControlPlane\Auth\CredentialAccessPolicyReadinessService.cs",
    "src\Migration.ControlPlane\Auth\CredentialAccessPolicyReadinessRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\CredentialAccessPolicyReadinessEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\credentialAccessPolicyReadiness.ts",
    "tools\test\smoke-credential-access-policy-readiness.ps1",
    "tools\test\smoke-credential-access-policy-readiness.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_045_CREDENTIAL_ACCESS_POLICY_READINESS.md"
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

if ($program -notmatch "AddCredentialAccessPolicyReadiness") {
    if ($program -match "builder\.Services\.AddEndpointPolicyInventory\(\);") {
        $program = $program.Replace(
            "builder.Services.AddEndpointPolicyInventory();",
            "builder.Services.AddEndpointPolicyInventory();`r`nbuilder.Services.AddCredentialAccessPolicyReadiness();")
        Write-Host "Patched Program.cs credential access policy readiness registration."
    }
    else {
        throw "Could not find endpoint policy inventory registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapCredentialAccessPolicyReadinessEndpoints") {
    if ($program -match "api\.MapEndpointPolicyInventoryEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapEndpointPolicyInventoryEndpoints();",
            "api.MapEndpointPolicyInventoryEndpoints();`r`napi.MapCredentialAccessPolicyReadinessEndpoints();")
        Write-Host "Patched Program.cs credential access policy readiness endpoints."
    }
    else {
        throw "Could not find endpoint policy inventory endpoint mapping anchor in Program.cs."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 045 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-credential-access-policy-readiness.ps1 -BaseUrl http://localhost:5173"
