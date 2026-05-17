$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set043-auth-policy-readiness"

Write-Host "Applying P2 Set 043 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Auth\AuthPolicyReadinessContracts.cs",
    "src\Migration.ControlPlane\Auth\IAuthPolicyReadinessService.cs",
    "src\Migration.ControlPlane\Auth\AuthPolicyReadinessService.cs",
    "src\Migration.ControlPlane\Auth\AuthPolicyReadinessRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\AuthPolicyReadinessEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\authPolicyReadiness.ts",
    "tools\test\smoke-auth-policy-readiness.ps1",
    "tools\test\smoke-auth-policy-readiness.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_043_AUTH_POLICY_READINESS.md"
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

if ($program -notmatch "AddAuthPolicyReadiness") {
    if ($program -match "builder\.Services\.AddOperationalReadiness\(\);") {
        $program = $program.Replace(
            "builder.Services.AddOperationalReadiness();",
            "builder.Services.AddOperationalReadiness();`r`nbuilder.Services.AddAuthPolicyReadiness();")
        Write-Host "Patched Program.cs auth policy readiness registration."
    }
    else {
        throw "Could not find operational readiness registration anchor in Program.cs."
    }
}

if ($program -notmatch "MapAuthPolicyReadinessEndpoints") {
    if ($program -match "api\.MapOperationalReadinessEndpoints\(\);") {
        $program = $program.Replace(
            "api.MapOperationalReadinessEndpoints();",
            "api.MapOperationalReadinessEndpoints();`r`napi.MapAuthPolicyReadinessEndpoints();")
        Write-Host "Patched Program.cs auth policy readiness endpoints."
    }
    else {
        throw "Could not find operational readiness endpoint mapping anchor in Program.cs."
    }
}

if ($program -notmatch "using Migration\.ControlPlane\.Auth;") {
    $program = "using Migration.ControlPlane.Auth;`r`n" + $program
    Write-Host "Patched Program.cs using Migration.ControlPlane.Auth;"
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 043 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then validate after starting Admin API:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-auth-policy-readiness.ps1 -BaseUrl http://localhost:5173"
