[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath = (Join-Path $RepoRoot 'config-samples\runtime-deployment-evidence.template.json'),

    [Parameter(Mandatory = $false)]
    [string] $OutputDirectory = (Join-Path $RepoRoot 'artifacts\runtime-deployment-evidence')
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BasePath,

        [Parameter(Mandatory = $true)]
        [string] $PathValue
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return Join-Path $BasePath $PathValue
}

if (-not (Test-Path -LiteralPath $ConfigurationPath)) {
    throw ('Evidence configuration file was not found: {0}' -f $ConfigurationPath)
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
$properties = $config.PSObject.Properties
if ($null -eq $properties['evidenceFiles']) {
    throw 'Evidence configuration is missing evidenceFiles.'
}

$items = @()
foreach ($property in $config.evidenceFiles.PSObject.Properties) {
    $relativeOrAbsolutePath = [string] $property.Value
    $fullPath = Resolve-FullPath -BasePath $RepoRoot -PathValue $relativeOrAbsolutePath
    $exists = Test-Path -LiteralPath $fullPath
    $length = 0
    if ($exists) {
        $item = Get-Item -LiteralPath $fullPath
        if (-not $item.PSIsContainer) {
            $length = [int64] $item.Length
        }
    }

    $items += [pscustomobject]@{
        Name = $property.Name
        Path = $relativeOrAbsolutePath
        FullPath = $fullPath
        Exists = [bool] $exists
        Length = $length
    }
}

$summary = [pscustomobject]@{
    EnvironmentName = $config.environmentName
    RepoCommit = $config.repoCommit
    RepoTag = $config.repoTag
    DeploymentUtc = $config.deploymentUtc
    GeneratedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    Evidence = $items
}

$jsonPath = Join-Path $OutputDirectory 'runtime-deployment-evidence.json'
$markdownPath = Join-Path $OutputDirectory 'runtime-deployment-evidence.md'

$json = ConvertTo-Json -InputObject $summary -Depth 8
Set-Content -LiteralPath $jsonPath -Value $json -Encoding UTF8

$lines = @()
$lines += '# Runtime Deployment Evidence'
$lines += ''
$lines += ('- Environment: {0}' -f $summary.EnvironmentName)
$lines += ('- Repo commit: {0}' -f $summary.RepoCommit)
$lines += ('- Repo tag: {0}' -f $summary.RepoTag)
$lines += ('- Deployment UTC: {0}' -f $summary.DeploymentUtc)
$lines += ('- Generated UTC: {0}' -f $summary.GeneratedUtc)
$lines += ''
$lines += '## Evidence files'
$lines += ''
$lines += '| Name | Exists | Length | Path |'
$lines += '| --- | --- | ---: | --- |'
foreach ($item in $items) {
    $lines += ('| {0} | {1} | {2} | `{3}` |' -f $item.Name, $item.Exists, $item.Length, $item.Path)
}

Set-Content -LiteralPath $markdownPath -Value $lines -Encoding UTF8

Write-Host ('Wrote evidence JSON: {0}' -f $jsonPath)
Write-Host ('Wrote evidence report: {0}' -f $markdownPath)
