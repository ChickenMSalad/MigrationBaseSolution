[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $RepoRoot,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $ConfigurationPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}

if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
}

if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $RepoRoot 'config-samples\p10-real-migration-execution-candidate.sample.json'
}

if (-not [System.IO.Path]::IsPathRooted($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $RepoRoot $ConfigurationPath
}

if (-not (Test-Path -LiteralPath $ConfigurationPath)) {
    throw ('Candidate configuration file is missing: {0}' -f $ConfigurationPath)
}

$config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json

foreach ($propertyName in @('candidateName', 'jobDefinitionPath', 'manifestPath', 'mappingProfilePath', 'sourceRoot', 'targetRoot', 'expectedSourceFiles', 'expectedJobTypes')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Candidate configuration is missing property: {0}' -f $propertyName)
    }
}

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $RepoRoot $Path
}

$jobPath = Resolve-RepoPath -Path ([string] $config.jobDefinitionPath)
$manifestPath = Resolve-RepoPath -Path ([string] $config.manifestPath)
$mappingPath = Resolve-RepoPath -Path ([string] $config.mappingProfilePath)
$sourceRoot = Resolve-RepoPath -Path ([string] $config.sourceRoot)

foreach ($path in @($jobPath, $manifestPath, $mappingPath, $sourceRoot)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('Required candidate path is missing: {0}' -f $path)
    }
}

$job = Get-Content -LiteralPath $jobPath -Raw | ConvertFrom-Json
$expected = $config.expectedJobTypes

$checks = @(
    @{ Name = 'SourceType'; Expected = [string] $expected.sourceType },
    @{ Name = 'TargetType'; Expected = [string] $expected.targetType },
    @{ Name = 'ManifestType'; Expected = [string] $expected.manifestType }
)

foreach ($check in $checks) {
    $property = $job.PSObject.Properties[$check.Name]
    if ($null -eq $property) {
        throw ('Job definition is missing property: {0}' -f $check.Name)
    }

    if (-not [string]::Equals([string] $property.Value, [string] $check.Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw ('Job definition {0} expected {1} but found {2}.' -f $check.Name, $check.Expected, $property.Value)
    }
}

$manifestLines = @(Get-Content -LiteralPath $manifestPath)
if ($manifestLines.Count -lt 2) {
    throw 'Candidate manifest must include a header and at least one data row.'
}

$header = $manifestLines[0]
foreach ($requiredColumn in @('RowId', 'SourceAssetId', 'SourcePath')) {
    if ($header.IndexOf($requiredColumn, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Candidate manifest is missing required column: {0}' -f $requiredColumn)
    }
}

foreach ($sourceFile in @($config.expectedSourceFiles)) {
    $sourcePath = Resolve-RepoPath -Path ([string] $sourceFile)
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw ('Expected source fixture file is missing: {0}' -f $sourceFile)
    }
}

Write-Host ('P10.0A real migration candidate validation passed: {0}' -f $config.candidateName)
