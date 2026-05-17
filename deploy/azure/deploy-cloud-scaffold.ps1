param(
    [string]$ResourceGroupName,
    [string]$Location = "eastus",
    [string]$EnvironmentName = "dev",
    [string]$NamePrefix = "migration",
    [string]$QueueExecutorImage = "migration-queue-executor:local",
    [switch]$WhatIf,
    [switch]$SkipStorage,
    [switch]$SkipKeyVault,
    [switch]$SkipAppService,
    [switch]$SkipWorker
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ResourceGroupName)) {
    throw "ResourceGroupName is required."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

Write-Host "MigrationBaseSolution Azure deployment orchestration"
Write-Host "Resource group : $ResourceGroupName"
Write-Host "Location       : $Location"
Write-Host "Environment    : $EnvironmentName"
Write-Host "Name prefix    : $NamePrefix"
Write-Host "WhatIf         : $WhatIf"
Write-Host ""

function Invoke-Step {
    param(
        [string]$Name,
        [string]$ScriptPath,
        [string[]]$Arguments
    )

    Write-Host ""
    Write-Host "== $Name =="
    Write-Host "$ScriptPath $($Arguments -join ' ')"

    if (!(Test-Path $ScriptPath)) {
        throw "Expected script not found: $ScriptPath"
    }

    & powershell -ExecutionPolicy Bypass -File $ScriptPath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

$common = @(
    "-ResourceGroupName", $ResourceGroupName,
    "-Location", $Location,
    "-EnvironmentName", $EnvironmentName,
    "-NamePrefix", $NamePrefix
)

if ($WhatIf) {
    $common += "-WhatIf"
}

if (!$SkipStorage) {
    Invoke-Step `
        -Name "Storage" `
        -ScriptPath ".\deploy\azure\storage\deploy-storage.ps1" `
        -Arguments $common
}

if (!$SkipKeyVault) {
    Invoke-Step `
        -Name "Key Vault" `
        -ScriptPath ".\deploy\azure\key-vault\deploy-key-vault.ps1" `
        -Arguments $common
}

if (!$SkipAppService) {
    Invoke-Step `
        -Name "Admin API App Service" `
        -ScriptPath ".\deploy\azure\app-service\deploy-app-service.ps1" `
        -Arguments $common
}

if (!$SkipWorker) {
    $workerArgs = $common + @("-QueueExecutorImage", $QueueExecutorImage)

    Invoke-Step `
        -Name "Queue Executor Container Apps Job" `
        -ScriptPath ".\deploy\azure\container-apps-job\deploy-container-apps-job.ps1" `
        -Arguments $workerArgs
}

Write-Host ""
Write-Host "Azure deployment orchestration completed."
Write-Host ""
Write-Host "Next manual steps:"
Write-Host "  1. Capture deployment outputs from Azure CLI."
Write-Host "  2. Generate appsettings with tools\cloud\new-cloud-appsettings-from-outputs.ps1."
Write-Host "  3. Review RBAC principal/resource ids."
Write-Host "  4. Run deploy\azure\rbac\deploy-managed-identity-rbac.ps1 with WhatIf."
Write-Host "  5. Deploy application package/container image."
Write-Host "  6. Validate /health and /api/cloud/readiness."
