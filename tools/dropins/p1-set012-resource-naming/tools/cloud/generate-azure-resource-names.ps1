param(
    [string]$Environment = "dev",
    [string]$Workspace = "default",
    [string]$Region = "eastus",
    [string]$Prefix = "migration"
)

function Normalize-Segment {
    param([string]$Value)

    $normalized = $Value.ToLowerInvariant() -replace '[^a-z0-9-]', '-'
    while ($normalized.Contains("--")) {
        $normalized = $normalized.Replace("--", "-")
    }

    return $normalized.Trim('-')
}

$environment = Normalize-Segment $Environment
$workspace = Normalize-Segment $Workspace
$prefix = Normalize-Segment $Prefix

Write-Host ""
Write-Host "Azure Resource Naming Plan"
Write-Host "--------------------------"
Write-Host "Environment : $environment"
Write-Host "Workspace   : $workspace"
Write-Host "Region      : $Region"
Write-Host ""

$storage = "$prefix$environment""sa"
$keyVault = "$prefix-$environment-kv"
$appService = "$prefix-$environment-admin-api"
$queue = "$prefix-runs-$environment"
$artifactContainer = "$prefix-artifacts-$environment"
$controlPlaneContainer = "$prefix-control-plane-$environment"

Write-Host ("Storage Account        : {0}" -f $storage)
Write-Host ("Key Vault              : {0}" -f $keyVault)
Write-Host ("Admin API App Service  : {0}" -f $appService)
Write-Host ("Queue Name             : {0}" -f $queue)
Write-Host ("Artifact Container     : {0}" -f $artifactContainer)
Write-Host ("Control Plane Container: {0}" -f $controlPlaneContainer)
Write-Host ""

Write-Host "Workspace-scoped examples"
Write-Host ("Artifact Root          : workspaces/{0}/artifacts" -f $workspace)
Write-Host ("Runs Root              : workspaces/{0}/runs" -f $workspace)
Write-Host ("Projects Root          : workspaces/{0}/projects" -f $workspace)
