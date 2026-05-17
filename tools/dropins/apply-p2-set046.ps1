$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set046-auth-enforcement-diagnostics"

Write-Host "Applying P2 Set 046 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Auth\AuthEnforcementDiagnosticsContracts.cs",
    "src\Migration.ControlPlane\Auth\IAuthEnforcementDiagnosticsService.cs",
    "src\Migration.ControlPlane\Auth\AuthEnforcementDiagnosticsService.cs",
    "src\Migration.ControlPlane\Auth\AuthEnforcementDiagnosticsRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\AuthEnforcementDiagnosticsEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\authEnforcementDiagnostics.ts",
    "tools\test\smoke-auth-enforcement-diagnostics.ps1",
    "tools\test\smoke-auth-enforcement-diagnostics.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_046_AUTH_ENFORCEMENT_DIAGNOSTICS.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "AddAuthEnforcementDiagnostics") {
    $program = $program.Replace(
        "builder.Services.AddCredentialAccessPolicyReadiness();",
        "builder.Services.AddCredentialAccessPolicyReadiness();`r`nbuilder.Services.AddAuthEnforcementDiagnostics();")
}

if ($program -notmatch "MapAuthEnforcementDiagnosticsEndpoints") {
    $program = $program.Replace(
        "api.MapCredentialAccessPolicyReadinessEndpoints();",
        "api.MapCredentialAccessPolicyReadinessEndpoints();`r`napi.MapAuthEnforcementDiagnosticsEndpoints();")
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 046 applied."
