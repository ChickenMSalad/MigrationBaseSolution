param(
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$SkipHttp,
    [switch]$Strict
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

Write-Host "MigrationBaseSolution cloud diagnostics validation"
Write-Host "Repo root: $repoRoot"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$failed = $false

function Assert-FileExists {
    param([string]$Path)

    if (!(Test-Path $Path)) {
        Write-Host "FAIL missing file: $Path" -ForegroundColor Red
        $script:failed = $true
        return
    }

    Write-Host "OK file: $Path"
}

function Test-JsonFile {
    param([string]$Path)

    Assert-FileExists $Path

    if (!(Test-Path $Path)) {
        return
    }

    try {
        Get-Content $Path -Raw | ConvertFrom-Json | Out-Null
        Write-Host "OK json: $Path"
    }
    catch {
        Write-Host "FAIL invalid json: $Path - $($_.Exception.Message)" -ForegroundColor Red
        $script:failed = $true
    }
}

function Invoke-CloudDiagnostic {
    param(
        [string]$Path,
        [string[]]$RequiredProperties
    )

    $url = "$BaseUrl$Path"
    Write-Host "GET $url"

    try {
        $result = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 15

        foreach ($property in $RequiredProperties) {
            if ($null -eq $result.PSObject.Properties[$property]) {
                Write-Host "FAIL $Path missing property '$property'" -ForegroundColor Red
                $script:failed = $true
            }
        }

        Write-Host "OK $Path"
    }
    catch {
        Write-Host "FAIL $Path - $($_.Exception.Message)" -ForegroundColor Red
        $script:failed = $true
    }
}

Write-Host "Checking environment templates..."
$templateFiles = @(
    "config\environments\local-dev.appsettings.example.json",
    "config\environments\dev.appsettings.example.json",
    "config\environments\test.appsettings.example.json",
    "config\environments\prod.appsettings.example.json",
    "config\environments\README.md"
)

foreach ($file in $templateFiles) {
    if ($file.EndsWith(".json")) {
        Test-JsonFile $file
    }
    else {
        Assert-FileExists $file
    }
}

Write-Host ""

Write-Host "Checking cloud tooling/docs..."
$expectedFiles = @(
    "tools\cloud\generate-azure-resource-names.ps1",
    "tools\cloud\show-promotion-checklist.ps1",
    "docs\azure\AZURE_RESOURCE_NAMING.md",
    "docs\azure\AZURE_ENVIRONMENT_PROMOTION_CHECKLIST.md",
    "docs\azure\FRONTEND_AUTH_BOOTSTRAP.md",
    "docs\azure\BACKEND_AUTH_SCAFFOLD.md"
)

foreach ($file in $expectedFiles) {
    Assert-FileExists $file
}

Write-Host ""

if (!$SkipHttp) {
    Write-Host "Checking cloud diagnostic endpoints..."
    Invoke-CloudDiagnostic "/api/cloud/environment" @("environmentName", "hostKind", "storageMode", "queueProvider", "warnings")
    Invoke-CloudDiagnostic "/api/workspace/context" @("workspaceId", "tenantMode", "isTenantEnforced", "warnings")
    Invoke-CloudDiagnostic "/api/workspace/storage-plan" @("workspaceId", "storageMode", "workspaceRoot", "artifactsRoot", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/credential-provider-plan" @("credentialMode", "providerKind", "secretNamePrefix", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/artifact-storage-plan" @("artifactMode", "providerKind", "artifactRoot", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/queue-provider-plan" @("queueProvider", "providerKind", "logicalQueueName", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/deployment-profile" @("profileName", "hostingModel", "requiredConfigurationKeys", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/configuration-audit" @("maturityLevel", "configuredCount", "keys", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/readiness" @("isCloudReady", "warningCount", "checks", "warnings")

    Write-Host ""
    Write-Host "Checking operational/auth diagnostic endpoints..."
    Invoke-CloudDiagnostic "/health/live" @("status", "checkedUtc")
    Invoke-CloudDiagnostic "/health/ready" @("status", "environmentName", "checks", "warnings")
    Invoke-CloudDiagnostic "/health/cloud" @("status", "environmentName", "checks", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/telemetry/correlation" @("environmentName", "correlationId", "recommendedHeaders", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/audit/event-contract" @("auditMode", "providerKind", "supportedEventTypes", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/auth/policy-plan" @("authMode", "authRequired", "roles", "routePolicies", "warnings")
    Invoke-CloudDiagnostic "/api/cloud/auth/configuration" @("authMode", "authRequired", "isConfigured", "warnings")
}
else {
    Write-Host "Skipping HTTP endpoint checks because -SkipHttp was supplied."
}

Write-Host ""

if ($failed) {
    if ($Strict) {
        throw "Cloud diagnostics validation failed."
    }

    Write-Host "Cloud diagnostics validation completed with failures." -ForegroundColor Yellow
    exit 1
}

Write-Host "Cloud diagnostics validation completed successfully."
