[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
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
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

if ([string]::IsNullOrWhiteSpace($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $RepoRoot 'config-samples\runtime-ci-release-gate.sample.json'
}
if (-not [System.IO.Path]::IsPathRooted($ConfigurationPath)) {
    $ConfigurationPath = Join-Path $RepoRoot $ConfigurationPath
}
if (-not (Test-Path -LiteralPath $ConfigurationPath)) {
    throw ('Configuration file not found: {0}' -f $ConfigurationPath)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot 'artifacts\runtime-ci-release-gate'
}
if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot $OutputPath
}
if (-not (Test-Path -LiteralPath $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json

$results = @()
$requiredFiles = @()
if ($null -ne $config.PSObject.Properties['requiredFiles']) {
    $requiredFiles = @($config.requiredFiles)
}

foreach ($requiredFile in $requiredFiles) {
    $requiredPath = Join-Path $RepoRoot ([string]$requiredFile)
    if (Test-Path -LiteralPath $requiredPath) {
        $results += [pscustomobject]@{
            Name = [string]$requiredFile
            Type = 'RequiredFile'
            Status = 'Passed'
            Message = 'Present.'
        }
    }
    else {
        $results += [pscustomobject]@{
            Name = [string]$requiredFile
            Type = 'RequiredFile'
            Status = 'Failed'
            Message = 'Missing.'
        }
    }
}

$validators = @()
if ($null -ne $config.PSObject.Properties['validators']) {
    $validators = @($config.validators)
}

foreach ($validator in $validators) {
    $validatorRelativePath = [string]$validator
    $validatorPath = Join-Path $RepoRoot $validatorRelativePath
    if (-not (Test-Path -LiteralPath $validatorPath)) {
        $results += [pscustomobject]@{
            Name = $validatorRelativePath
            Type = 'Validator'
            Status = 'Failed'
            Message = 'Validator file not found.'
        }
        continue
    }

    try {
        & $validatorPath | Out-Null
        $results += [pscustomobject]@{
            Name = $validatorRelativePath
            Type = 'Validator'
            Status = 'Passed'
            Message = 'Completed successfully.'
        }
    }
    catch {
        $results += [pscustomobject]@{
            Name = $validatorRelativePath
            Type = 'Validator'
            Status = 'Failed'
            Message = $_.Exception.Message
        }
    }
}

$failedCount = @($results | Where-Object { $_.Status -eq 'Failed' }).Count
$passedCount = @($results | Where-Object { $_.Status -eq 'Passed' }).Count
$generatedUtc = [DateTimeOffset]::UtcNow.ToString('O')

$reportPath = Join-Path $OutputPath 'runtime-ci-release-gate-report.md'
$lines = @()
$lines += '# Runtime CI Release Gate Report'
$lines += ''
$lines += ('- Generated UTC: {0}' -f $generatedUtc)
$lines += ('- Repository root: {0}' -f $RepoRoot)
$lines += ('- Passed: {0}' -f $passedCount)
$lines += ('- Failed: {0}' -f $failedCount)
$lines += ''
$lines += '| Type | Name | Status | Message |'
$lines += '| --- | --- | --- | --- |'
foreach ($result in $results) {
    $message = ([string]$result.Message).Replace('|', '\|')
    $lines += ('| {0} | `{1}` | {2} | {3} |' -f $result.Type, $result.Name, $result.Status, $message)
}

Set-Content -LiteralPath $reportPath -Value $lines -Encoding UTF8
Write-Host ('Runtime CI release gate report written to {0}' -f $reportPath)

if ($failedCount -gt 0) {
    throw ('Runtime CI release gate failed. Failed checks: {0}' -f $failedCount)
}
