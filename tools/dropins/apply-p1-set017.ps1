$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set017-authorization-policy-plan"

Write-Host "Applying P1 Set 017 from $repoRoot"

$files = @(
    "src\Migration.Admin.Api\Contracts\AuthorizationPolicyPlanContracts.cs",
    "src\Migration.Admin.Api\Endpoints\AuthorizationPolicyPlanEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\authorizationPolicyPlan.ts",
    "docs\cloud-roadmap-cleanup\P1_SET_017_AUTHORIZATION_POLICY_PLAN.md"
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

if ($program -notmatch "MapAuthorizationPolicyPlanEndpoints") {
    if ($program -match "api\.MapCloudPlatformEndpoints\(\);") {
        $program = $program -replace "api\.MapCloudPlatformEndpoints\(\);", "api.MapCloudPlatformEndpoints();`r`napi.MapAuthorizationPolicyPlanEndpoints();"
        Set-Content -Path $programPath -Value $program -Encoding UTF8
        Write-Host "Patched Program.cs after api.MapCloudPlatformEndpoints();"
    }
    elseif ($program -match "api\.MapAuditEventContractEndpoints\(\);") {
        $program = $program -replace "api\.MapAuditEventContractEndpoints\(\);", "api.MapAuditEventContractEndpoints();`r`napi.MapAuthorizationPolicyPlanEndpoints();"
        Set-Content -Path $programPath -Value $program -Encoding UTF8
        Write-Host "Patched Program.cs after api.MapAuditEventContractEndpoints();"
    }
    else {
        throw "Could not find cloud endpoint mapping anchor in Program.cs. No partial patch was written."
    }
}
else {
    Write-Host "Program.cs already maps authorization policy plan endpoints."
}

Write-Host ""
Write-Host "P1 Set 017 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
Write-Host "Then verify:"
Write-Host "  Invoke-RestMethod http://localhost:5173/api/cloud/auth/policy-plan"
