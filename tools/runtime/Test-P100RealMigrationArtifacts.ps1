[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath = 'config-samples\p10-localstorage-real-migration.sample.json'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BasePath,

        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    if ([System.IO.Path]::IsPathRooted($RelativePath)) {
        return $RelativePath
    }

    return [System.IO.Path]::Combine($BasePath, $RelativePath)
}

$repoFullPath = (Resolve-Path -LiteralPath $RepoRoot).Path
$configPath = Resolve-RepoPath -BasePath $repoFullPath -RelativePath $ConfigurationPath

if (-not (Test-Path -LiteralPath $configPath)) {
    throw ('Configuration file not found: {0}' -f $configPath)
}

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json

foreach ($propertyName in @('jobDefinitionPath', 'mappingProfilePath', 'manifestPath', 'sourceRoot', 'targetRoot', 'expectedWorkType')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Configuration is missing required property: {0}' -f $propertyName)
    }
}

$jobPath = Resolve-RepoPath -BasePath $repoFullPath -RelativePath $config.jobDefinitionPath
$mappingPath = Resolve-RepoPath -BasePath $repoFullPath -RelativePath $config.mappingProfilePath
$manifestPath = Resolve-RepoPath -BasePath $repoFullPath -RelativePath $config.manifestPath
$sourceRoot = Resolve-RepoPath -BasePath $repoFullPath -RelativePath $config.sourceRoot
$targetRoot = Resolve-RepoPath -BasePath $repoFullPath -RelativePath $config.targetRoot

foreach ($pathToCheck in @($jobPath, $mappingPath, $manifestPath, $sourceRoot, $targetRoot)) {
    if (-not (Test-Path -LiteralPath $pathToCheck)) {
        throw ('Required P10.0B artifact path is missing: {0}' -f $pathToCheck)
    }
}

$job = Get-Content -LiteralPath $jobPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('jobName', 'sourceType', 'targetType', 'manifestType', 'mappingProfilePath')) {
    if ($null -eq $job.PSObject.Properties[$propertyName]) {
        throw ('Job definition is missing required property: {0}' -f $propertyName)
    }
}

if ($job.sourceType -ne 'LocalStorage') {
    throw ('Expected sourceType LocalStorage but found: {0}' -f $job.sourceType)
}

if ($job.targetType -ne 'LocalStorage') {
    throw ('Expected targetType LocalStorage but found: {0}' -f $job.targetType)
}

if ($job.manifestType -ne 'Csv') {
    throw ('Expected manifestType Csv for the real LocalStorage candidate but found: {0}' -f $job.manifestType)
}

$mapping = Get-Content -LiteralPath $mappingPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('profileName', 'sourceType', 'targetType')) {
    if ($null -eq $mapping.PSObject.Properties[$propertyName]) {
        throw ('Mapping profile is missing required property: {0}' -f $propertyName)
    }
}

if ($mapping.sourceType -ne $job.sourceType) {
    throw 'Mapping sourceType does not match job sourceType.'
}

if ($mapping.targetType -ne $job.targetType) {
    throw 'Mapping targetType does not match job targetType.'
}

$manifestLines = @(Get-Content -LiteralPath $manifestPath)
if ($manifestLines.Count -lt 2) {
    throw 'Manifest candidate must contain a header row and at least one data row.'
}

Write-Host 'P10.0B real migration artifact validation passed.'
