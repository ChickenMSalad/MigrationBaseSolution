[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigPath = (Join-Path $RepoRoot 'config-samples\p10-site-cloud-readiness.sample.json')
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Assert-PathExists {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ('Missing {0}: {1}' -f $Description, $Path)
    }
}

$repoFullPath = $RepoRoot
if (-not [System.IO.Path]::IsPathRooted($repoFullPath)) {
    $repoFullPath = Join-Path (Get-Location).Path $repoFullPath
}
$repoFullPath = [System.IO.Path]::GetFullPath($repoFullPath)

$configFullPath = $ConfigPath
if (-not [System.IO.Path]::IsPathRooted($configFullPath)) {
    $configFullPath = Join-Path $repoFullPath $configFullPath
}
$configFullPath = [System.IO.Path]::GetFullPath($configFullPath)

Assert-PathExists -Path $configFullPath -Description 'P10.1A config sample'
$config = Get-Content -LiteralPath $configFullPath -Raw | ConvertFrom-Json

foreach ($propertyName in @('frontendPath', 'adminApiPath', 'requiredCloudSettings', 'expectedRuntimeState')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Config sample is missing property: {0}' -f $propertyName)
    }
}

$frontendPath = Join-Path $repoFullPath ([string]$config.frontendPath)
$adminApiPath = Join-Path $repoFullPath ([string]$config.adminApiPath)

Assert-PathExists -Path $frontendPath -Description 'frontend path'
Assert-PathExists -Path $adminApiPath -Description 'admin API path'

$requiredSettings = @($config.requiredCloudSettings)
foreach ($settingName in @('ConnectionStrings__MigrationOperationalStore', 'SqlOperationalRuntimeReadiness__ConnectionString')) {
    if ($requiredSettings -notcontains $settingName) {
        throw ('Config sample is missing required cloud setting: {0}' -f $settingName)
    }
}

$runtimeState = $config.expectedRuntimeState
foreach ($propertyName in @('runsTable', 'workItemsTable', 'manifestRowsTable')) {
    if ($null -eq $runtimeState.PSObject.Properties[$propertyName]) {
        throw ('expectedRuntimeState is missing property: {0}' -f $propertyName)
    }
}

[pscustomobject]@{
    RepoRoot = $repoFullPath
    FrontendPath = $frontendPath
    AdminApiPath = $adminApiPath
    RequiredSettings = ($requiredSettings -join ', ')
}
