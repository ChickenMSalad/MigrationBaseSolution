param(
    [string]$Version = "0.1.0-local",
    [string]$EnvironmentName = "dev",
    [string]$OutputPath = "artifacts/release/release-manifest.json"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

function Get-GitValue {
    param([string[]]$Arguments)

    try {
        $value = & git @Arguments 2>$null
        if ($LASTEXITCODE -eq 0) {
            return ($value | Select-Object -First 1)
        }
    }
    catch {
    }

    return $null
}

$commit = Get-GitValue @("rev-parse", "HEAD")
$branch = Get-GitValue @("rev-parse", "--abbrev-ref", "HEAD")
$status = Get-GitValue @("status", "--porcelain")

$publishManifestPath = "artifacts/publish/publish-manifest.json"
$publishManifestExists = Test-Path $publishManifestPath

$manifest = [ordered]@{
    schemaVersion = "1.0"
    generatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    version = $Version
    environmentName = $EnvironmentName
    git = [ordered]@{
        branch = $branch
        commit = $commit
        hasUncommittedChanges = -not [string]::IsNullOrWhiteSpace($status)
    }
    artifacts = [ordered]@{
        publishManifestPath = $publishManifestPath
        publishManifestExists = $publishManifestExists
        adminApiPath = "artifacts/publish/admin-api"
        queueExecutorPath = "artifacts/publish/queue-executor"
        adminWebDistPath = "src/Admin/Migration.Admin.Web/dist"
    }
    diagnostics = [ordered]@{
        readinessEndpoint = "/api/cloud/readiness"
        configurationAuditEndpoint = "/api/cloud/configuration-audit"
        healthLiveEndpoint = "/health/live"
        healthReadyEndpoint = "/health/ready"
        healthCloudEndpoint = "/health/cloud"
    }
    deployment = [ordered]@{
        azureOrchestrationScript = "deploy/azure/deploy-cloud-scaffold.ps1"
        promotionChecklist = "docs/azure/AZURE_ENVIRONMENT_PROMOTION_CHECKLIST.md"
        generatedAppsettingsScript = "tools/cloud/new-cloud-appsettings-from-outputs.ps1"
        rbacScript = "deploy/azure/rbac/deploy-managed-identity-rbac.ps1"
    }
}

$outputDirectory = Split-Path $OutputPath -Parent
if (![string]::IsNullOrWhiteSpace($outputDirectory) -and !(Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputPath -Encoding UTF8

Write-Host "Release manifest generated:"
Write-Host "  $OutputPath"
