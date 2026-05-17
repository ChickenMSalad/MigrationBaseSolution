param(
    [string]$ReleaseManifestPath = "artifacts/release/release-manifest.json",
    [switch]$RequirePublishArtifacts,
    [switch]$Strict
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$failed = $false

function Assert-Exists {
    param(
        [string]$Path,
        [string]$Description,
        [switch]$Required
    )

    if (Test-Path $Path) {
        Write-Host "OK   $Description`: $Path"
        return
    }

    if ($Required) {
        Write-Host "FAIL $Description missing: $Path" -ForegroundColor Red
        $script:failed = $true
    }
    else {
        Write-Host "WARN $Description missing: $Path" -ForegroundColor Yellow
    }
}

function Assert-Json {
    param([string]$Path)

    if (!(Test-Path $Path)) {
        return
    }

    try {
        Get-Content $Path -Raw | ConvertFrom-Json | Out-Null
        Write-Host "OK   valid json: $Path"
    }
    catch {
        Write-Host "FAIL invalid json: $Path - $($_.Exception.Message)" -ForegroundColor Red
        $script:failed = $true
    }
}

Write-Host "MigrationBaseSolution release readiness validation"
Write-Host "Repo root: $repoRoot"
Write-Host ""

Assert-Exists $ReleaseManifestPath "release manifest" -Required
Assert-Json $ReleaseManifestPath

Assert-Exists "tools\build\publish-cloud-artifacts.ps1" "publish script" -Required
Assert-Exists "tools\release\new-release-manifest.ps1" "release manifest script" -Required
Assert-Exists "tools\cloud\validate-cloud-diagnostics.ps1" "cloud diagnostics validator" -Required

Assert-Exists "deploy\azure\deploy-cloud-scaffold.ps1" "Azure orchestration script" -Required
Assert-Exists "deploy\azure\storage\main.bicep" "storage Bicep" -Required
Assert-Exists "deploy\azure\key-vault\main.bicep" "Key Vault Bicep" -Required
Assert-Exists "deploy\azure\app-service\main.bicep" "App Service Bicep" -Required
Assert-Exists "deploy\azure\container-apps-job\main.bicep" "worker job Bicep" -Required
Assert-Exists "deploy\azure\rbac\managed-identity-rbac.bicep" "RBAC Bicep" -Required

Assert-Exists "docs\azure\AZURE_ENVIRONMENT_PROMOTION_CHECKLIST.md" "promotion checklist" -Required
Assert-Exists "docs\azure\AZURE_DEPLOYMENT_ORCHESTRATION.md" "deployment orchestration docs" -Required
Assert-Exists "docs\azure\RELEASE_MANIFEST.md" "release manifest docs" -Required

if ($RequirePublishArtifacts) {
    Assert-Exists "artifacts\publish\admin-api" "Admin API publish output" -Required
    Assert-Exists "artifacts\publish\queue-executor" "Queue Executor publish output" -Required
    Assert-Exists "artifacts\publish\publish-manifest.json" "publish manifest" -Required
    Assert-Json "artifacts\publish\publish-manifest.json"
}
else {
    Assert-Exists "artifacts\publish\admin-api" "Admin API publish output"
    Assert-Exists "artifacts\publish\queue-executor" "Queue Executor publish output"
    Assert-Exists "artifacts\publish\publish-manifest.json" "publish manifest"
}

Write-Host ""

if ($failed) {
    if ($Strict) {
        throw "Release readiness validation failed."
    }

    Write-Host "Release readiness validation completed with failures." -ForegroundColor Yellow
    exit 1
}

Write-Host "Release readiness validation completed successfully."
