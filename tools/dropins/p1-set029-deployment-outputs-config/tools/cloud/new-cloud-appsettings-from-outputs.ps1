param(
    [Parameter(Mandatory=$true)]
    [string]$EnvironmentName,

    [string]$WorkspaceId = $EnvironmentName,

    [string]$DeploymentProfile = $EnvironmentName,

    [string]$Region = "eastus",

    [string]$HostKind = "azureAppService",

    [string]$Sku = "B1",

    [string]$StorageAccountName,

    [string]$ArtifactContainerName,

    [string]$ControlPlaneContainerName,

    [string]$RunQueueName,

    [string]$KeyVaultUri,

    [string]$AuthAuthority,

    [string]$AuthAudience,

    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

function Require-Value {
    param(
        [string]$Name,
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name is required."
    }
}

Require-Value "EnvironmentName" $EnvironmentName
Require-Value "StorageAccountName" $StorageAccountName
Require-Value "ArtifactContainerName" $ArtifactContainerName
Require-Value "ControlPlaneContainerName" $ControlPlaneContainerName
Require-Value "RunQueueName" $RunQueueName

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = "config\environments\$EnvironmentName.generated.appsettings.json"
}

$config = [ordered]@{
    Cloud = [ordered]@{
        DeploymentProfile = $DeploymentProfile
        HostKind = $HostKind
        Region = $Region
        Sku = $Sku
        CredentialMode = "managedIdentity"
        ArtifactMode = "azureBlob"
        KeyVaultUri = $KeyVaultUri
        ArtifactContainerName = $ArtifactContainerName
        ArtifactStorageAccountName = $StorageAccountName
        QueueStorageAccountName = $StorageAccountName
        RequiresHttps = $true
        RequiresAuth = $true
        RequiresPrivateNetworking = $false
        EnablesDiagnostics = $true
        EnablesHealthProbes = $true
    }
    Workspace = [ordered]@{
        TenantMode = "singleTenant"
        TenantEnforced = $false
        WorkspaceId = $WorkspaceId
        DisplayName = "$EnvironmentName Workspace"
    }
    ControlPlane = [ordered]@{
        StorageRoot = "az://$ControlPlaneContainerName"
    }
    MigrationRunQueue = [ordered]@{
        Provider = "AzureQueue"
        QueueName = $RunQueueName
        StorageAccountName = $StorageAccountName
    }
    Auth = [ordered]@{
        Authority = $AuthAuthority
        Audience = $AuthAudience
    }
}

$outputDirectory = Split-Path $OutputPath -Parent
if (![string]::IsNullOrWhiteSpace($outputDirectory) -and !(Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$config | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputPath -Encoding UTF8

Write-Host "Generated cloud appsettings file:"
Write-Host "  $OutputPath"
Write-Host ""
Write-Host "Review before committing or applying to Azure App Service configuration."
